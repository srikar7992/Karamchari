// -----------------------------------------------------------------------
// <copyright file="PayrollRecoveryIntegrationTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Results;
using Karamchari.Payroll.Services.Calculation;
using Karamchari.Payroll.Services.Deductions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Testcontainers.MsSql;
using Xunit;
using Xunit.Abstractions;

namespace Karamchari.Payroll.IntegrationTests.Recovery;

/// <summary>
/// DB-backed recovery validation.
/// Proves that resumability holds against real SQL Server:
///   - Persist first N results to DB
///   - Simulate crash (stop mid-run)
///   - Restart: query DB for completed IDs, skip them
///   - Persist remainder
///   - Verify: no duplicates (unique constraint), no omissions, net pay unchanged
/// </summary>
[Collection(nameof(PayrollSqlServerCollection))]
public sealed class PayrollRecoveryIntegrationTests(
    PayrollSqlServerFixture fixture,
    ITestOutputHelper output)
{
    private static readonly TaxCalculatorService TaxSvc = new();

    private static PayrollCalculationEngine Engine() => new([
        new ProvidentFundCalculator(),
        new EsiCalculator(),
        new ProfessionalTaxCalculator(),
    ]);

    private static StatutoryConfigSnapshot Stat() =>
        new(15_000m, 21_000m, 0.0075m, 0.0325m, 0.12m, 0.0367m, 1_250m);

    private static (Guid id, EmployeePayrollInput input) BuildEmployee(int index)
    {
        var id = Guid.NewGuid();
        var salary = 25_000m + (index % 100) * 1_000m;
        var att = Enumerable.Range(0, 26).Select(i =>
            new DayAttendanceRecord(new DateOnly(2025, 4, 1).AddDays(i),
                8m + (i % 3 == 0 ? 2m : 0m), true, false, false, null, null)).ToList();
        var input = new EmployeePayrollInput(
            id, $"Emp_{index}", salary, Math.Round(salary / 208m, 2), "INR", 40m, 0m,
            att, Array.Empty<HolidayRecord>(),
            new OvertimePolicySnapshot([new OvertimeRuleSnapshot(0m, null, 1.5m, "Regular", 1)]),
            Array.Empty<ShiftPremiumSnapshot>(), Array.Empty<PendingAdjustmentRecord>(),
            false, false, "KA", "New", 0m, 0m, 0m, 4, 2025,
            Guid.NewGuid(), "FY2024-25-V1", Stat());
        return (id, input);
    }

    private static EmployeePayrollResult ToResult(
        Guid runId, Guid empId, string empName, EmployeePayrollResult calc)
    {
        // Reconstruct from calc — in production PayrollBatchConsumer does this.
        return EmployeePayrollResult.Calculate(
            "test-tenant", runId, empId, empName, "Apr 2025", 1, "snap",
            new EarningsBreakdown(calc.BasePay, calc.OvertimePay, calc.HolidayPay,
                calc.ShiftPremium, 0m, 0m),
            new DeductionsBreakdown(calc.ProvidentFund, calc.EmployeeStateInsurance,
                calc.ProfessionalTax, calc.TaxDeductedAtSource, 0m),
            0m, 0m);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task Recovery_PersistFirst400_RestartPersistRemaining600_NoDuplicates()
    {
        const int total = 1_000;
        const int crashAt = 400;
        var runId = Guid.NewGuid();
        var engine = Engine();

        var employees = Enumerable.Range(0, total).Select(BuildEmployee).ToList();
        var netPayByEmployee = new Dictionary<Guid, decimal>(total);

        // ── Phase 1: calculate + persist first 400 ───────────────────────────
        await using var db1 = fixture.CreateDbContext();
        var phase1 = employees.Take(crashAt).ToList();

        foreach (var (id, input) in phase1)
        {
            var calc = engine.Calculate("test-tenant", runId, "Apr 2025", 1, "snap", input, TaxSvc);
            var result = ToResult(runId, id, input.EmployeeName, calc);
            netPayByEmployee[id] = result.NetPay;
            db1.EmployeePayrollResults.Add(result);
        }
        await db1.SaveChangesAsync();

        output.WriteLine($"Phase 1: persisted {crashAt} results to DB");

        // ── Simulate restart: query DB for already-done IDs ──────────────────
        await using var db2 = fixture.CreateDbContext();
        var alreadyDoneIds = await db2.EmployeePayrollResults
            .AsNoTracking()
            .Where(r => r.PayrollRunId == runId)
            .Select(r => r.EmployeeId)
            .ToListAsync();

        alreadyDoneIds.Should().HaveCount(crashAt, "DB must return exactly the persisted count");
        var alreadyDone = alreadyDoneIds.ToHashSet();

        var pending = employees.Where(e => !alreadyDone.Contains(e.id)).ToList();
        pending.Should().HaveCount(total - crashAt, "remaining should be 600");

        // ── Phase 2: calculate + persist remaining 600 ───────────────────────
        foreach (var (id, input) in pending)
        {
            var calc = engine.Calculate("test-tenant", runId, "Apr 2025", 1, "snap", input, TaxSvc);
            var result = ToResult(runId, id, input.EmployeeName, calc);
            netPayByEmployee[id] = result.NetPay;
            db2.EmployeePayrollResults.Add(result);
        }
        await db2.SaveChangesAsync();

        output.WriteLine($"Phase 2: persisted {total - crashAt} more results");

        // ── Verify: total rows, no duplicates, all employees present ─────────
        await using var db3 = fixture.CreateDbContext();
        var allRows = await db3.EmployeePayrollResults
            .AsNoTracking()
            .Where(r => r.PayrollRunId == runId)
            .Select(r => new { r.EmployeeId, r.NetPay })
            .ToListAsync();

        allRows.Should().HaveCount(total,
            "every employee should have exactly one result row");

        var uniqueEmployees = allRows.Select(r => r.EmployeeId).ToHashSet();
        uniqueEmployees.Should().HaveCount(total, "no duplicate employee IDs");

        foreach (var (empId, expectedNet) in netPayByEmployee)
        {
            var row = allRows.Single(r => r.EmployeeId == empId);
            row.NetPay.Should().Be(expectedNet,
                $"net pay for {empId} must be identical on re-read");
        }

        output.WriteLine($"Verified: {allRows.Count} rows, {uniqueEmployees.Count} unique employees, all net pays match");
    }

    [DockerRequiredFact]
    public async Task Recovery_UniqueConstraint_BlocksDuplicateInsert()
    {
        var runId = Guid.NewGuid();
        var (empId, input) = BuildEmployee(0);
        var engine = Engine();

        await using var db = fixture.CreateDbContext();

        var calc = engine.Calculate("test-tenant", runId, "Apr 2025", 1, "snap", input, TaxSvc);
        var result1 = ToResult(runId, empId, input.EmployeeName, calc);
        db.EmployeePayrollResults.Add(result1);
        await db.SaveChangesAsync();

        // Duplicate: same RunId + EmployeeId
        var result2 = ToResult(runId, empId, input.EmployeeName, calc);
        db.EmployeePayrollResults.Add(result2);

        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<Exception>(
            "SQL unique constraint on (PayrollRunId, EmployeeId) must reject duplicate");
    }

    [DockerRequiredFact]
    public async Task Recovery_DifferentRuns_SameEmployee_BothSucceed()
    {
        // Two separate runs for same employee → each gets its own row (different RunId)
        var runA = Guid.NewGuid();
        var runB = Guid.NewGuid();
        var (empId, input) = BuildEmployee(1);
        var engine = Engine();

        await using var db = fixture.CreateDbContext();

        var calcA = engine.Calculate("test-tenant", runA, "Apr 2025", 1, "snap", input, TaxSvc);
        db.EmployeePayrollResults.Add(ToResult(runA, empId, input.EmployeeName, calcA));

        var calcB = engine.Calculate("test-tenant", runB, "May 2025", 2, "snap", input, TaxSvc);
        db.EmployeePayrollResults.Add(ToResult(runB, empId, input.EmployeeName, calcB));

        var act = async () => await db.SaveChangesAsync();
        await act.Should().NotThrowAsync("different RunIds for same employee are allowed");

        var rows = await db.EmployeePayrollResults
            .AsNoTracking()
            .Where(r => r.EmployeeId == empId)
            .CountAsync();

        rows.Should().Be(2);
    }

    [DockerRequiredFact]
    public async Task Recovery_ResumeQuery_IsIdempotent_ReturnsConsistentSet()
    {
        // Querying alreadyDone twice returns identical result (no race conditions in read path)
        var runId = Guid.NewGuid();
        var engine = Engine();

        await using var db = fixture.CreateDbContext();

        var employees = Enumerable.Range(0, 50).Select(BuildEmployee).ToList();
        foreach (var (id, input) in employees)
        {
            var calc = engine.Calculate("test-tenant", runId, "Apr 2025", 1, "snap", input, TaxSvc);
            db.EmployeePayrollResults.Add(ToResult(runId, id, input.EmployeeName, calc));
        }
        await db.SaveChangesAsync();

        // Query twice
        var first = await db.EmployeePayrollResults.AsNoTracking()
            .Where(r => r.PayrollRunId == runId).Select(r => r.EmployeeId).ToListAsync();

        var second = await db.EmployeePayrollResults.AsNoTracking()
            .Where(r => r.PayrollRunId == runId).Select(r => r.EmployeeId).ToListAsync();

        first.Should().BeEquivalentTo(second, "resume query must be stable");
    }

    [DockerRequiredFact]
    public async Task Recovery_1000Employees_Persist_And_Query_Under10Seconds()
    {
        const int count = 1_000;
        var runId = Guid.NewGuid();
        var engine = Engine();
        var employees = Enumerable.Range(0, count).Select(BuildEmployee).ToList();

        var sw = Stopwatch.StartNew();

        // Batch insert: 100 at a time (mirrors chunk(100) in consumer)
        foreach (var batch in employees.Chunk(100))
        {
            await using var db = fixture.CreateDbContext();
            foreach (var (id, input) in batch)
            {
                var calc = engine.Calculate("test-tenant", runId, "Apr 2025", 1, "snap", input, TaxSvc);
                db.EmployeePayrollResults.Add(ToResult(runId, id, input.EmployeeName, calc));
            }
            await db.SaveChangesAsync();
        }

        var insertMs = sw.ElapsedMilliseconds;

        // Resume query
        await using var dbRead = fixture.CreateDbContext();
        var doneIds = await dbRead.EmployeePayrollResults
            .AsNoTracking()
            .Where(r => r.PayrollRunId == runId)
            .Select(r => r.EmployeeId)
            .ToListAsync();

        sw.Stop();

        output.WriteLine($"Insert 1000 (batches of 100): {insertMs}ms");
        output.WriteLine($"Resume query: {sw.ElapsedMilliseconds - insertMs}ms");
        output.WriteLine($"Total: {sw.ElapsedMilliseconds}ms");

        doneIds.Should().HaveCount(count);
        sw.ElapsedMilliseconds.Should().BeLessThan(10_000,
            $"1000 employees persist + resume query took {sw.ElapsedMilliseconds}ms, expected < 10s");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Fixture: SQL Server container + PayrollDbContext with EnsureCreated
// ─────────────────────────────────────────────────────────────────────────────

[CollectionDefinition(nameof(PayrollSqlServerCollection))]
public sealed class PayrollSqlServerCollection : ICollectionFixture<PayrollSqlServerFixture>;

public sealed class PayrollSqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container;

    public PayrollSqlServerFixture()
    {
        // Testcontainers MatchImage uses [GeneratedRegex] which inherits the AppDomain
        // REGEX_DEFAULT_MATCH_TIMEOUT. Under parallel test load the default can be a
        // finite value and the image-name regex times out before completing. Force infinite.
        AppContext.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", Timeout.InfiniteTimeSpan);
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    }

    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Pooling = false,
            TrustServerCertificate = true,
        };
        _connectionString = builder.ConnectionString;

        // Create the placeholder schema that KaramchariDbContext maps all entities to
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{KaramchariDbContext.PlaceholderSchema}')
                EXEC(N'CREATE SCHEMA [{KaramchariDbContext.PlaceholderSchema}]');
            """;
        await cmd.ExecuteNonQueryAsync();

        // EnsureCreated builds the full payroll schema (tables + unique indexes)
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public PayrollDbContext CreateDbContext()
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns("test-tenant");
        tenantProvider.GetTenant().Returns(new TenantExecutionEnvelope(
            "test-tenant", "corr", "req", ExecutionSource.Test, TenantSource.JwtClaim));

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        return new PayrollDbContext(options, tenantProvider);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Skip attribute: skip when Docker unavailable
// ─────────────────────────────────────────────────────────────────────────────

internal sealed class DockerRequiredFactAttribute : FactAttribute
{
    public DockerRequiredFactAttribute()
    {
        if (!IsContinuousIntegration() && !IsDockerAvailable())
        {
            Skip = "Docker is required. Start Docker or run in CI.";
        }
    }

    private static bool IsContinuousIntegration() =>
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);

    private static bool IsDockerAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                ArgumentList = { "info", "--format", "{{.ServerVersion}}" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            return process is not null && process.WaitForExit(5000) && process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception) { return false; }
        catch (InvalidOperationException) { return false; }
    }
}
