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
/// Elite survivability tests proving the system survives hostile operational reality (RULE 1E.11).
/// </summary>
public sealed class FinancialSurvivabilityTests : IDisposable
{
    private readonly FinancialOpsDbContext _db;
    private readonly ChaosExecutionTimeline _timeline;
    private readonly FinancialChaosEngine _chaosEngine;
    private readonly FinancialConsistencyGuard _consistencyGuard;
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FinancialSurvivabilityTests"/> class.
    /// </summary>
    public FinancialSurvivabilityTests()
    {
        // Use in-memory DB for deterministic chaos simulation
        var options = new DbContextOptionsBuilder<FinancialOpsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenantProvider = Substitute.For<ITenantProvider>();
        var tenantId = "acme-corp";
        var envelope = new TenantExecutionEnvelope(
            tenantId,
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.BackgroundJob,
            TenantSource.Provisioning);

        _tenantProvider.GetTenant().Returns(envelope);
        _tenantProvider.GetCurrentTenantId().Returns(tenantId);

        _db = new FinancialOpsDbContext(options, _tenantProvider);
        _timeline = new ChaosExecutionTimeline();

        _chaosEngine = new FinancialChaosEngine(
            _db,
            Substitute.For<ILogger<FinancialChaosEngine>>(),
            _timeline);

        _consistencyGuard = new FinancialConsistencyGuard(
            _db,
            Substitute.For<ILogger<FinancialConsistencyGuard>>());
    }

    /// <summary>
    /// Validates that reconciliation drift triggers a partition-scoped quarantine.
    /// </summary>
    [Fact]
    public async Task ScenarioReconciliationDriftShouldTriggerQuarantine()
    {
        // 1. Setup baseline truth
        var tenantId = "acme-corp";
        var periodId = FinancialOperationalPeriodId.New();

        var entry = WorkforceFinancialEntry.Record(
            tenantId, new EmployeeId(Guid.NewGuid()), periodId,
            FinancialOperationType.Reimbursement, 100.00m, CurrencyCode.USD,
            FinancialCorrelationChain.CreateNew(), FinancialReplayMetadata.Empty);

        _db.Set<WorkforceFinancialEntry>().Add(entry);

        var finalization = FinancialFinalizationSnapshot.Finalize(tenantId, periodId, 100.00m, "checksum-abc");
        _db.FinalizationSnapshots.Add(finalization);
        await _db.SaveChangesAsync();

        // 2. Inject Chaos: Reconciliation Drift (RULE 8)
        var scenario = FinancialChaosScenario.Create(
            FinancialChaosScenarioType.ReconciliationDrift,
            tenantId, periodId, "seed-123", FinancialChaosSeverity.Critical);

        await _chaosEngine.InjectScenarioAsync(scenario);

        // 3. Manually simulate the drift effect (corrupting DB bypassing domain logic)
        var entryToCorrupt = await _db.Set<WorkforceFinancialEntry>().IgnoreQueryFilters().FirstAsync();
        typeof(WorkforceFinancialEntry).GetProperty(nameof(entryToCorrupt.Amount))!.SetValue(entryToCorrupt, 150.00m);
        await _db.SaveChangesAsync();

        // 4. Validate: Consistency Guard must detect drift and trigger quarantine
        Func<Task> act = async () => await _consistencyGuard.ValidatePeriodConsistencyAsync(tenantId, periodId);

        await act.Should().ThrowAsync<FinancialConsistencyException>()
            .WithMessage("*mismatch*");

        // 5. Verify Timeline: Postmortem must reflect the incident
        var postmortem = _timeline.GetForScenario(scenario.ScenarioId);
        postmortem.Should().Contain(e => e.EventName == "LedgerCorruptionInjected");
    }

    /// <summary>
    /// Validates that projection lag is correctly tracked in the chaos timeline.
    /// </summary>
    [Fact]
    public async Task ScenarioProjectionLagShouldEmitOperationalTelemetry()
    {
        // 1. Setup
        var tenantId = "acme-corp";
        var periodId = FinancialOperationalPeriodId.New();

        var scenario = FinancialChaosScenario.Create(
            FinancialChaosScenarioType.ProjectionLag,
            tenantId, periodId, "seed-lag-1", FinancialChaosSeverity.Degraded);

        // 2. Inject Chaos: Projection Lag (RULE 13)
        await _chaosEngine.InjectScenarioAsync(scenario);

        // 3. Validate Telemetry (RULE 4)
        var events = _timeline.GetForScenario(scenario.ScenarioId);
        events.Should().Contain(e => e.EventName == "ProjectionLagInjected");
        events.First(e => e.EventName == "ProjectionLagInjected").ReplaySafe.Should().BeTrue();
    }

    /// <summary>
    /// Validates that human error (incorrect replay target) is rejected.
    /// </summary>
    [Fact]
    public async Task ScenarioHumanErrorIncorrectReplayTargetShouldBeRejected()
    {
        // 1. Setup
        var tenantId = "acme-corp";
        var periodId = FinancialOperationalPeriodId.New();

        var scenario = FinancialChaosScenario.Create(
            FinancialChaosScenarioType.IncorrectReplayTarget,
            tenantId, periodId, "seed-human-1");

        // 2. Inject Chaos: Operator tries to replay a finalized partition
        await _chaosEngine.InjectScenarioAsync(scenario);

        // 3. Validate Replay Safety (RULE 9)
        var events = _timeline.GetForScenario(scenario.ScenarioId);

        events.Should().Contain(e => e.EventName == "OperatorMistakeInjected");
        events.First(e => e.EventName == "OperatorMistakeInjected").ReplaySafe.Should().BeFalse();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _db.Dispose();
    }
}
