using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.DataMigration.Features.SalaryRevision;
using Karamchari.DataMigration.Services;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.SalaryRevisions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Diagnostics.Internal;
using NSubstitute;
using Xunit;

namespace Karamchari.DataMigration.Tests.Unit.Processors;

public sealed class SalaryRevisionImportProcessorTests : IDisposable
{
    private readonly IReferenceResolver _resolver = Substitute.For<IReferenceResolver>();
    private readonly ITenantProvider _tenantProvider = Substitute.For<ITenantProvider>();
    private readonly PayrollDbContext _db;
    private readonly SalaryRevisionImportProcessor _sut;

    private static readonly Guid EmployeeId = Guid.NewGuid();
    private const string TenantId = "tenant-test-001";
    private const string EmployeeName = "Arjun Sharma";

    public SalaryRevisionImportProcessorTests()
    {
        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
            {
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning);
            })
            .Options;

        _tenantProvider.GetCurrentTenantId().Returns(TenantId);
        _tenantProvider.TryGetCurrentTenantId(out Arg.Any<string?>())
            .Returns(x => { x[0] = TenantId; return true; });

        // Build a TenantExecutionEnvelope substitute for GetTenant()
        var envelope = new TenantExecutionEnvelope(
            TenantId,
            correlationId: "test-correlation",
            requestId: "test-request",
            executionSource: ExecutionSource.HttpRequest,
            source: TenantSource.JwtClaim);
        _tenantProvider.GetTenant().Returns(envelope);
        _tenantProvider.TryGetTenant(out Arg.Any<TenantExecutionEnvelope?>())
            .Returns(x => { x[0] = envelope; return true; });

        _db = new PayrollDbContext(options, _tenantProvider);

        _resolver.ResolveEmployeeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);
        _resolver.ResolveEmployeeNameAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        _sut = new SalaryRevisionImportProcessor(_resolver, _db, _tenantProvider);
    }

    public void Dispose() => _db.Dispose();

    private void SetupEmployeeResolved(string empNumber = "EMP001")
    {
        _resolver.ResolveEmployeeIdAsync(empNumber, Arg.Any<CancellationToken>())
            .Returns(EmployeeId);
        _resolver.ResolveEmployeeNameAsync(EmployeeId, Arg.Any<CancellationToken>())
            .Returns(EmployeeName);
    }

    private static SalaryRevisionImportRecord MakeRecord(
        string employeeNumber = "EMP001",
        decimal previousCTC = 800000m,
        decimal newCTC = 1000000m,
        string effectiveDate = "2024-04-01",
        string revisionType = "AnnualIncrement",
        string? remarks = null) => new()
        {
            EmployeeNumber = employeeNumber,
            PreviousCTC = previousCTC,
            NewCTC = newCTC,
            EffectiveDate = effectiveDate,
            RevisionType = revisionType,
            Remarks = remarks
        };

    // ------------------------------------------------------------------ ImportType

    [Fact]
    public void ImportType_ReturnsSalaryRevisionImport()
    {
        _sut.ImportType.Should().Be("SalaryRevisionImport");
    }

    // ------------------------------------------------------------------ empty batch

    [Fact]
    public async Task ProcessBatchAsync_EmptyBatch_NoRevisionsSaved()
    {
        await _sut.ProcessBatchAsync(Array.Empty<SalaryRevisionImportRecord>());

        var count = await _db.SalaryRevisions.CountAsync();
        count.Should().Be(0);
    }

    // ------------------------------------------------------------------ single item

    [Fact]
    public async Task ProcessBatchAsync_SingleItem_InsertsOneRevision()
    {
        SetupEmployeeResolved();

        await _sut.ProcessBatchAsync([MakeRecord()]);

        var count = await _db.SalaryRevisions.IgnoreQueryFilters().CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task ProcessBatchAsync_SingleItem_EmployeeIdResolvedCorrectly()
    {
        SetupEmployeeResolved();

        await _sut.ProcessBatchAsync([MakeRecord()]);

        var revision = await _db.SalaryRevisions.IgnoreQueryFilters().SingleAsync();
        revision.EmployeeId.Should().Be(EmployeeId);
    }

    [Fact]
    public async Task ProcessBatchAsync_SingleItem_TenantIdSetCorrectly()
    {
        SetupEmployeeResolved();

        await _sut.ProcessBatchAsync([MakeRecord()]);

        var revision = await _db.SalaryRevisions.IgnoreQueryFilters().SingleAsync();
        revision.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public async Task ProcessBatchAsync_SingleItem_NewCTCSetCorrectly()
    {
        SetupEmployeeResolved();

        await _sut.ProcessBatchAsync([MakeRecord(newCTC: 1200000m)]);

        var revision = await _db.SalaryRevisions.IgnoreQueryFilters().SingleAsync();
        revision.NewCTC.Should().Be(1200000m);
    }

    [Fact]
    public async Task ProcessBatchAsync_SingleItem_PreviousCTCSetCorrectly()
    {
        SetupEmployeeResolved();

        await _sut.ProcessBatchAsync([MakeRecord(previousCTC: 900000m)]);

        var revision = await _db.SalaryRevisions.IgnoreQueryFilters().SingleAsync();
        revision.PreviousCTC.Should().Be(900000m);
    }

    [Fact]
    public async Task ProcessBatchAsync_SingleItem_NullPreviousCTCDefaultsToZero()
    {
        SetupEmployeeResolved();
        var record = MakeRecord() with { PreviousCTC = null };

        await _sut.ProcessBatchAsync([record]);

        var revision = await _db.SalaryRevisions.IgnoreQueryFilters().SingleAsync();
        revision.PreviousCTC.Should().Be(0m);
    }

    // ------------------------------------------------------------------ revision type

    [Theory]
    [InlineData("AnnualIncrement", RevisionType.AnnualIncrement)]
    [InlineData("annualincrement", RevisionType.AnnualIncrement)]
    [InlineData("Promotion", RevisionType.Promotion)]
    [InlineData("MarketCorrection", RevisionType.MarketCorrection)]
    [InlineData("RetentionAdjustment", RevisionType.RetentionAdjustment)]
    [InlineData("OffCycle", RevisionType.OffCycle)]
    public async Task ProcessBatchAsync_RevisionTypeParsedFromString(string typeString, RevisionType expected)
    {
        SetupEmployeeResolved();
        var record = MakeRecord(revisionType: typeString);

        await _sut.ProcessBatchAsync([record]);

        var revision = await _db.SalaryRevisions.IgnoreQueryFilters().SingleAsync();
        revision.Type.Should().Be(expected);
    }

    [Fact]
    public async Task ProcessBatchAsync_UnknownRevisionType_FallsBackToOffCycle()
    {
        SetupEmployeeResolved();
        var record = MakeRecord(revisionType: "SomethingUnknown");

        await _sut.ProcessBatchAsync([record]);

        var revision = await _db.SalaryRevisions.IgnoreQueryFilters().SingleAsync();
        revision.Type.Should().Be(RevisionType.OffCycle);
    }

    [Fact]
    public async Task ProcessBatchAsync_NullRevisionType_FallsBackToOffCycle()
    {
        SetupEmployeeResolved();
        var record = MakeRecord() with { RevisionType = null };

        await _sut.ProcessBatchAsync([record]);

        var revision = await _db.SalaryRevisions.IgnoreQueryFilters().SingleAsync();
        revision.Type.Should().Be(RevisionType.OffCycle);
    }

    // ------------------------------------------------------------------ effective date

    [Fact]
    public async Task ProcessBatchAsync_ValidEffectiveDate_ParsedCorrectly()
    {
        SetupEmployeeResolved();
        var record = MakeRecord(effectiveDate: "2024-07-01");

        await _sut.ProcessBatchAsync([record]);

        var revision = await _db.SalaryRevisions.IgnoreQueryFilters().SingleAsync();
        revision.EffectiveFrom.Should().Be(new DateOnly(2024, 7, 1));
    }

    [Fact]
    public async Task ProcessBatchAsync_NullEffectiveDate_FallsBackToToday()
    {
        SetupEmployeeResolved();
        var record = MakeRecord() with { EffectiveDate = null };

        await _sut.ProcessBatchAsync([record]);

        var revision = await _db.SalaryRevisions.IgnoreQueryFilters().SingleAsync();
        revision.EffectiveFrom.Should().Be(DateOnly.FromDateTime(DateTime.Today));
    }

    // ------------------------------------------------------------------ unresolved employee

    [Fact]
    public async Task ProcessBatchAsync_UnresolvedEmployee_SkipsRecord()
    {
        _resolver.ResolveEmployeeIdAsync("EMP001", Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        await _sut.ProcessBatchAsync([MakeRecord()]);

        var count = await _db.SalaryRevisions.IgnoreQueryFilters().CountAsync();
        count.Should().Be(0);
    }

    // ------------------------------------------------------------------ batch of 3

    [Fact]
    public async Task ProcessBatchAsync_ThreeResolvedItems_ThreeRevisionsSaved()
    {
        _resolver.ResolveEmployeeIdAsync("EMP001", Arg.Any<CancellationToken>()).Returns(EmployeeId);
        _resolver.ResolveEmployeeIdAsync("EMP002", Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        _resolver.ResolveEmployeeIdAsync("EMP003", Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        _resolver.ResolveEmployeeNameAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(EmployeeName);

        var records = new[]
        {
            MakeRecord("EMP001"),
            MakeRecord("EMP002"),
            MakeRecord("EMP003")
        };

        await _sut.ProcessBatchAsync(records);

        var count = await _db.SalaryRevisions.IgnoreQueryFilters().CountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public async Task ProcessBatchAsync_MixedResolvable_OnlyResolvableItemsSaved()
    {
        _resolver.ResolveEmployeeIdAsync("EMP001", Arg.Any<CancellationToken>()).Returns(EmployeeId);
        _resolver.ResolveEmployeeIdAsync("EMP002", Arg.Any<CancellationToken>()).Returns((Guid?)null);
        _resolver.ResolveEmployeeNameAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(EmployeeName);

        var records = new[]
        {
            MakeRecord("EMP001"),
            MakeRecord("EMP002")
        };

        await _sut.ProcessBatchAsync(records);

        var count = await _db.SalaryRevisions.IgnoreQueryFilters().CountAsync();
        count.Should().Be(1);
    }
}
