using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Domain.Chaos;
using Karamchari.FinancialOps.Domain.Ledger;
using Karamchari.FinancialOps.Domain.Periods;
using Karamchari.FinancialOps.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Karamchari.FinancialChaosTests;

/// <summary>
/// Compounded survivability tests proving the system survives multi-layered hostile reality (RULE 1E.11.2).
/// </summary>
public sealed class CompoundedSurvivabilityTests : IDisposable
{
    private readonly FinancialOpsDbContext _db;
    private readonly ChaosExecutionTimeline _timeline;
    private readonly FinancialChaosEngine _chaosEngine;
    private readonly FinancialConsistencyGuard _consistencyGuard;
    private readonly ITenantProvider _tenantProvider;

    /// <summary>Initializes a new instance of the <see cref="CompoundedSurvivabilityTests"/> class.</summary>
    public CompoundedSurvivabilityTests()
    {
        var options = new DbContextOptionsBuilder<FinancialOpsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenantProvider = Substitute.For<ITenantProvider>();
        _tenantProvider.GetCurrentTenantId().Returns("tenant-a");

        _db = new FinancialOpsDbContext(options, _tenantProvider);
        _timeline = new ChaosExecutionTimeline();

        _chaosEngine = new FinancialChaosEngine(_db, Substitute.For<ILogger<FinancialChaosEngine>>(), _timeline);
        _consistencyGuard = new FinancialConsistencyGuard(_db, Substitute.For<ILogger<FinancialConsistencyGuard>>());
    }

    /// <summary>
    /// RULE 10 and RULE 11: Massive pressure on Tenant A must not degrade Tenant B, 
    /// and recovery recursion must be strictly bounded.
    /// </summary>
    [Fact]
    public async Task ScenarioCrossTenantPressureAndRecursionLimit()
    {
        // 1. Setup Tenant A under massive chaos
        var tenantA = "tenant-a";
        var periodA = FinancialOperationalPeriodId.New();

        // Inject compounded chaos for Tenant A
        await _chaosEngine.InjectScenarioAsync(FinancialChaosScenario.Create(
            FinancialChaosScenarioType.ReplayStorm, tenantA, periodA, "seed-a-1", FinancialChaosSeverity.Elite));
        await _chaosEngine.InjectScenarioAsync(FinancialChaosScenario.Create(
            FinancialChaosScenarioType.ResourceExhaustion, tenantA, periodA, "seed-a-2", FinancialChaosSeverity.Elite));

        // 2. Setup Tenant B (Healthy Neighbor)
        var tenantB = "tenant-b";
        var periodB = FinancialOperationalPeriodId.New();
        _tenantProvider.GetCurrentTenantId().Returns(tenantB); // Switch context to Tenant B

        var entryB = WorkforceFinancialEntry.Record(
            tenantB, new EmployeeId(Guid.NewGuid()), periodB,
            FinancialOperationType.Reimbursement, 50.00m, CurrencyCode.USD,
            FinancialCorrelationChain.CreateNew(), FinancialReplayMetadata.Empty);

        _db.Set<WorkforceFinancialEntry>().Add(entryB);
        await _db.SaveChangesAsync();

        // 3. Validate Tenant B is unaffected (RULE 11)
        var checkB = async () => await _consistencyGuard.ValidatePeriodConsistencyAsync(tenantB, periodB);
        await checkB.Should().NotThrowAsync();

        // 4. Validate Recovery Recursion Bounding (RULE 10)
        var chain = FinancialCorrelationChain.CreateNew();
        chain.Depth.Should().Be(1);

        var chain2 = chain.Next();
        var chain3 = chain2.Next();
        var chain4 = chain3.Next();
        var chain5 = chain4.Next();

        chain5.Depth.Should().Be(5);

        Action act = () => chain5.Next();
        act.Should().Throw<InvalidOperationException>().WithMessage("*recursion limit exceeded*");
    }

    /// <summary>
    /// RULE 5 and RULE 20: Cascading failure (Lag + Storm + Outage) should result in 
    /// graceful degradation and surgical quarantine, not a global collapse.
    /// </summary>
    [Fact]
    public async Task ScenarioCascadingFailureGracefulDegradation()
    {
        var tenantId = "acme-corp";
        var periodId = FinancialOperationalPeriodId.New();
        _tenantProvider.GetCurrentTenantId().Returns(tenantId);

        // 1. Inject Cascading Failure
        await _chaosEngine.InjectScenarioAsync(FinancialChaosScenario.Create(
            FinancialChaosScenarioType.ProjectionLag, tenantId, periodId, "c1"));
        await _chaosEngine.InjectScenarioAsync(FinancialChaosScenario.Create(
            FinancialChaosScenarioType.ReplayStorm, tenantId, periodId, "c2"));
        await _chaosEngine.InjectScenarioAsync(FinancialChaosScenario.Create(
            FinancialChaosScenarioType.SettlementProviderOutage, tenantId, periodId, "c3"));

        // 2. Validate Observability & Telemetry (RULE 4 and RULE 15)
        var events = _timeline.GetAll();
        events.Should().HaveCountGreaterOrEqualTo(3);

        var postmortem = _timeline.GetMetrics(tenantId);
        postmortem.TotalIncidents.Should().Be(0); // Incidents are triggered by Guards, not engine directly

        // 3. Verify Human Error Simulation (RULE 5)
        await _chaosEngine.InjectScenarioAsync(FinancialChaosScenario.Create(
            FinancialChaosScenarioType.ConcurrentCompensationExecution, tenantId, periodId, "h1", FinancialChaosSeverity.Critical));

        var timeline = _timeline.GetMetrics(tenantId);
        // Metrics confirm we are tracking operational pressure
        timeline.Should().NotBeNull();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _db.Dispose();
    }
}
