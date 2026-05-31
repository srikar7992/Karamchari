using System.Text;
using FluentAssertions;
using Karamchari.DataMigration.Domain.Importing;
using Karamchari.DataMigration.Features.Employees;
using Karamchari.DataMigration.Features.HistoricalPayroll;
using Karamchari.DataMigration.Features.LeaveBalance;
using Karamchari.DataMigration.Features.SalaryComponent;
using Karamchari.DataMigration.Features.SalaryRevision;
using Karamchari.DataMigration.Services;
using Karamchari.DataMigration.Tests.Integration.Fixtures;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Karamchari.DataMigration.Tests.Integration;

/// <summary>
/// End-to-end tests that run the full import pipeline against a real SQL Server database.
/// Dependency cross-context calls (IReferenceResolver) are substituted.
/// </summary>
[Collection(nameof(ImportIntegrationCollection))]
public sealed class ImportPipelineEndToEndTests(ImportIntegrationFixture fixture)
{
    private static readonly Guid EmployeeGuid = Guid.NewGuid();
    private static readonly Guid PolicyGuid = Guid.NewGuid();

    // ── Employee Import ──────────────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task EmployeeImport_ValidCsv_PublishesOnboardCommands()
    {
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var processor = new EmployeeImportProcessor(publishEndpoint);
        var pipeline = BuildPipeline(new EmployeeImportMapper(), BuildEmployeeValidator(), processor);

        var csv = BuildEmployeeCsv(new[]
        {
            ("EMP001", "Alice", "Johnson", "alice@dev.local", "Engineering", "", "", "2024-01-01", "Full-Time"),
            ("EMP002", "Bob", "Smith", "bob@dev.local", "Engineering", "", "", "2024-01-15", "Full-Time"),
        });

        var parser = new CsvImportFileParser();
        var stream = ToStream(csv);
        var job = CreateJob("EmployeeImport");

        var (success, failed, _) = await pipeline.ProcessAsync(stream, parser, job);

        success.Should().Be(2);
        failed.Should().Be(0);
        await publishEndpoint.Received(2).Publish(Arg.Any<Karamchari.HR.Contracts.Employees.OnboardEmployeeCommand>(), Arg.Any<CancellationToken>());
    }

    [DockerRequiredFact]
    public async Task EmployeeImport_MissingRequiredField_CountsAsFailed()
    {
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var processor = new EmployeeImportProcessor(publishEndpoint);
        var pipeline = BuildPipeline(new EmployeeImportMapper(), BuildEmployeeValidator(), processor);

        // Row 2 has no email
        var csv = BuildEmployeeCsv(new[]
        {
            ("EMP001", "Alice", "Johnson", "alice@dev.local", "", "", "", "2024-01-01", ""),
            ("EMP002", "Bob", "Smith", "", "", "", "", "", ""),
        });

        var parser = new CsvImportFileParser();
        var (success, failed, _) = await pipeline.ProcessAsync(ToStream(csv), parser, CreateJob("EmployeeImport"));

        failed.Should().Be(1, "row 2 is missing email");
        success.Should().Be(1);
    }

    [DockerRequiredFact]
    public async Task EmployeeImport_Validate_ReturnsRowLevelErrors()
    {
        var pipeline = BuildPipeline(new EmployeeImportMapper(), BuildEmployeeValidator(),
            new EmployeeImportProcessor(Substitute.For<IPublishEndpoint>()));

        var csv = BuildEmployeeCsv(new[]
        {
            ("EMP001", "Alice", "Johnson", "alice@dev.local", "", "", "", "2024-01-01", ""),
            ("", "Bob", "Smith", "bob@dev.local", "", "", "", "", ""),   // no emp number
            ("EMP003", "", "", "", "", "", "", "", ""),                   // no first name, no email
        });

        var parser = new CsvImportFileParser();
        var (valid, invalid, errors) = await pipeline.ValidateAsync(ToStream(csv), parser);

        valid.Should().Be(1);
        invalid.Should().Be(2);
        errors.Should().HaveCount(2);
        // CsvImportFileParser numbers rows starting at 2 (header = row 1, first data row = 2).
        // Row 2 = EMP001 (valid), row 3 = missing emp number (invalid), row 4 = missing email (invalid).
        errors.Select(e => e.RowNumber).Should().BeEquivalentTo(new[] { 3, 4 });
    }

    // ── Leave Balance Import ─────────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task LeaveBalanceImport_ValidCsv_PersistsToTimeAttendanceContext()
    {
        // We substitute the full processor to avoid needing a real TimeAttendance DB.
        // The processor itself is covered in unit tests. Here we verify pipeline orchestration.
        var processorCalls = new List<LeaveBalanceImportRecord>();
        var fakeProcessor = Substitute.For<IImportProcessor<LeaveBalanceImportRecord>>();
        fakeProcessor.ImportType.Returns("LeaveBalanceImport");
        fakeProcessor.When(p => p.ProcessBatchAsync(Arg.Any<IEnumerable<LeaveBalanceImportRecord>>(), Arg.Any<CancellationToken>()))
                     .Do(ci => processorCalls.AddRange(ci.Arg<IEnumerable<LeaveBalanceImportRecord>>()));

        var resolver = BuildLeaveResolver();
        var pipeline = BuildPipeline(new LeaveBalanceImportMapper(), new LeaveBalanceImportValidator(resolver), fakeProcessor);

        var csv = BuildLeaveCsv(new[]
        {
            ("EMP001", "Annual Leave", "12.5", "2024-01-01"),
            ("EMP001", "Sick Leave", "8.0", "2024-01-01"),
        });

        var (success, failed, _) = await pipeline.ProcessAsync(ToStream(csv), new CsvImportFileParser(), CreateJob("LeaveBalanceImport"));

        success.Should().Be(2);
        failed.Should().Be(0);
        processorCalls.Should().HaveCount(2);
    }

    [DockerRequiredFact]
    public async Task LeaveBalanceImport_UnknownPolicy_CountsAsFailed()
    {
        var resolver = Substitute.For<IReferenceResolver>();
        resolver.ResolveEmployeeIdAsync("EMP001", Arg.Any<CancellationToken>()).Returns(EmployeeGuid);
        resolver.ResolveLeavePolicyIdAsync("Unknown Policy", Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var fakeProcessor = Substitute.For<IImportProcessor<LeaveBalanceImportRecord>>();
        fakeProcessor.ImportType.Returns("LeaveBalanceImport");
        var pipeline = BuildPipeline(new LeaveBalanceImportMapper(), new LeaveBalanceImportValidator(resolver), fakeProcessor);

        var csv = BuildLeaveCsv(new[] { ("EMP001", "Unknown Policy", "5.0", "2024-01-01") });
        var (success, failed, _) = await pipeline.ProcessAsync(ToStream(csv), new CsvImportFileParser(), CreateJob("LeaveBalanceImport"));

        failed.Should().Be(1);
        success.Should().Be(0);
        await fakeProcessor.DidNotReceive().ProcessBatchAsync(Arg.Any<IEnumerable<LeaveBalanceImportRecord>>(), Arg.Any<CancellationToken>());
    }

    // ── Salary Component Import ─────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task SalaryComponentImport_ValidCsv_PersistsRecords()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var processorCalls = new List<SalaryComponentImportRecord>();
        var fakeProcessor = Substitute.For<IImportProcessor<SalaryComponentImportRecord>>();
        fakeProcessor.ImportType.Returns("SalaryComponentImport");
        fakeProcessor.When(p => p.ProcessBatchAsync(Arg.Any<IEnumerable<SalaryComponentImportRecord>>(), Arg.Any<CancellationToken>()))
                     .Do(ci => processorCalls.AddRange(ci.Arg<IEnumerable<SalaryComponentImportRecord>>()));

        var resolver = Substitute.For<IReferenceResolver>();
        resolver.ResolveEmployeeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(EmployeeGuid);

        var pipeline = BuildPipeline(new SalaryComponentImportMapper(), new SalaryComponentImportValidator(resolver), fakeProcessor);

        var csv = BuildSalaryComponentCsv(new[]
        {
            ("EMP001", "Basic", "30000", "Monthly"),
            ("EMP001", "HRA", "12000", "Monthly"),
        });

        var (success, failed, _) = await pipeline.ProcessAsync(ToStream(csv), new CsvImportFileParser(), CreateJob("SalaryComponentImport"));

        success.Should().Be(2);
        failed.Should().Be(0);
        processorCalls.Should().HaveCount(2);
        processorCalls[0].ComponentName.Should().Be("Basic");
        processorCalls[1].ComponentName.Should().Be("HRA");
    }

    // ── Historical Payroll Import ────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task HistoricalPayrollImport_ValidCsv_PersistsToDataMigrationDb()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var resolver = Substitute.For<IReferenceResolver>();
        resolver.ResolveEmployeeIdAsync("E2EPAYROLL001", Arg.Any<CancellationToken>()).Returns(EmployeeGuid);
        resolver.ResolveEmployeeIdAsync("E2EPAYROLL002", Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());

        var processor = new HistoricalPayrollImportProcessor(db);
        var pipeline = BuildPipeline(new HistoricalPayrollImportMapper(), new HistoricalPayrollImportValidator(resolver), processor);

        var job = CreateJob("HistoricalPayrollImport");

        // Use test-specific employee numbers to avoid collision with other tests sharing the fixture DB.
        var csv = BuildPayrollCsv(new[]
        {
            ("E2EPAYROLL001", "6", "2020", "50000", "8000", "42000"),
            ("E2EPAYROLL002", "6", "2020", "60000", "10000", "50000"),
        });

        var (success, failed, _) = await pipeline.ProcessAsync(ToStream(csv), new CsvImportFileParser(), job);

        success.Should().Be(2);
        failed.Should().Be(0);

        var saved = await db.HistoricalPayrollSummaries
            .Where(s => s.EmployeeNumber == "E2EPAYROLL001" || s.EmployeeNumber == "E2EPAYROLL002")
            .AsNoTracking()
            .ToListAsync();
        saved.Should().HaveCount(2);
    }

    [DockerRequiredFact]
    public async Task HistoricalPayrollImport_InvalidMonth_CountsAsFailed()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);
        var resolver = Substitute.For<IReferenceResolver>();
        resolver.ResolveEmployeeIdAsync("EMP001", Arg.Any<CancellationToken>()).Returns(EmployeeGuid);

        var processor = new HistoricalPayrollImportProcessor(db);
        var pipeline = BuildPipeline(new HistoricalPayrollImportMapper(), new HistoricalPayrollImportValidator(resolver), processor);

        var csv = BuildPayrollCsv(new[] { ("EMP001", "13", "2024", "50000", "8000", "42000") });
        var (success, failed, _) = await pipeline.ProcessAsync(ToStream(csv), new CsvImportFileParser(), CreateJob("HistoricalPayrollImport"));

        failed.Should().Be(1);
        success.Should().Be(0);
    }

    // ── Preview ─────────────────────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task EmployeeImport_Preview_ReturnsAtMostTenRows()
    {
        var pipeline = BuildPipeline(new EmployeeImportMapper(), BuildEmployeeValidator(),
            new EmployeeImportProcessor(Substitute.For<IPublishEndpoint>()));

        // 15 rows in CSV
        var rows = Enumerable.Range(1, 15)
            .Select(i => ($"EMP{i:D3}", "First", "Last", $"emp{i}@test.com", "", "", "", "2024-01-01", "Full-Time"))
            .ToArray();
        var csv = BuildEmployeeCsv(rows);

        var preview = await pipeline.PreviewAsync(ToStream(csv), new CsvImportFileParser(), maxRows: 10);

        preview.Should().HaveCount(10);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ImportPipeline<T> BuildPipeline<T>(
        IImportMapper<T> mapper,
        IImportValidator<T> validator,
        IImportProcessor<T> processor) =>
        new(mapper, validator, processor, NullLogger<ImportPipeline<T>>.Instance);

    private static EmployeeImportValidator BuildEmployeeValidator()
    {
        var resolver = Substitute.For<IReferenceResolver>();
        resolver.ResolveEmployeeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(EmployeeGuid);
        resolver.ResolveDepartmentIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        return new EmployeeImportValidator(resolver);
    }

    private static IReferenceResolver BuildLeaveResolver()
    {
        var resolver = Substitute.For<IReferenceResolver>();
        resolver.ResolveEmployeeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(EmployeeGuid);
        resolver.ResolveLeavePolicyIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(PolicyGuid);
        return resolver;
    }

    private static ImportJob CreateJob(string importType) =>
        ImportJob.Create(importType, $"{importType}.csv", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"), "test");

    private static MemoryStream ToStream(string content) =>
        new(Encoding.UTF8.GetBytes(content));

    private static string BuildEmployeeCsv(IEnumerable<(string emp, string first, string last, string email, string dept, string desig, string mgr, string doj, string empType)> rows)
    {
        var sb = new StringBuilder("Employee Number,First Name,Last Name,Email,Department,Designation,Manager Employee Number,Date Of Joining,Employment Type\n");
        foreach (var r in rows) sb.AppendLine($"{r.emp},{r.first},{r.last},{r.email},{r.dept},{r.desig},{r.mgr},{r.doj},{r.empType}");
        return sb.ToString();
    }

    private static string BuildLeaveCsv(IEnumerable<(string emp, string policy, string balance, string date)> rows)
    {
        var sb = new StringBuilder("Employee Number,Leave Type,Balance,Effective Date\n");
        foreach (var r in rows) sb.AppendLine($"{r.emp},{r.policy},{r.balance},{r.date}");
        return sb.ToString();
    }

    private static string BuildSalaryComponentCsv(IEnumerable<(string emp, string comp, string amount, string freq)> rows)
    {
        var sb = new StringBuilder("Employee Number,Component,Amount,Frequency\n");
        foreach (var r in rows) sb.AppendLine($"{r.emp},{r.comp},{r.amount},{r.freq}");
        return sb.ToString();
    }

    private static string BuildPayrollCsv(IEnumerable<(string emp, string month, string year, string gross, string ded, string net)> rows)
    {
        var sb = new StringBuilder("Employee Number,Month,Year,Gross Pay,Deductions,Net Pay\n");
        foreach (var r in rows) sb.AppendLine($"{r.emp},{r.month},{r.year},{r.gross},{r.ded},{r.net}");
        return sb.ToString();
    }
}
