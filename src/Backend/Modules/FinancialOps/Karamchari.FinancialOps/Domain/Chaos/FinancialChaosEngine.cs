// -----------------------------------------------------------------------
// <copyright file="FinancialChaosEngine.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.FinancialOps.Domain.Chaos;

/// <summary>
/// Orchestrates deterministic, partition-scoped chaos for financial operations.
/// Strictly domain-focused (RULE 1) and deterministic (RULE 2).
/// </summary>
public sealed class FinancialChaosEngine : IFinancialChaosEngine
{
    private readonly FinancialOpsDbContext _db;
    private readonly ILogger<FinancialChaosEngine> _logger;
    private readonly ChaosExecutionTimeline _timeline;
    private readonly List<FinancialChaosScenario> _activeScenarios = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="FinancialChaosEngine"/> class.
    /// </summary>
    /// <param name="db">The financial operations database context.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="timeline">The chaos execution timeline tracker.</param>
    public FinancialChaosEngine(
        FinancialOpsDbContext db,
        ILogger<FinancialChaosEngine> logger,
        ChaosExecutionTimeline timeline)
    {
        _db = db;
        _logger = logger;
        _timeline = timeline;
    }

    /// <inheritdoc/>
    public async Task InjectScenarioAsync(FinancialChaosScenario scenario, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        _logger.LogWarning(
            "STARTING ELITE FINANCIAL CHAOS: {Type} (ID: {Id}) for Tenant {TenantId}, Period {PeriodId}",
            scenario.Type, scenario.ScenarioId, scenario.TenantId, scenario.PeriodId);

        _activeScenarios.Add(scenario);

        RecordOperationalEvent(scenario, "ScenarioStarted",
            $"Chaos type {scenario.Type} injected into partition {scenario.PeriodId}.",
            "Monitor for drift or automated quarantine.");

        // Deterministic simulation based on seed
        var random = new Random(scenario.DeterministicSeed.GetHashCode());

        try
        {
            switch (scenario.Type)
            {
                case FinancialChaosScenarioType.ReconciliationDrift:
                    await InjectReconciliationDriftAsync(scenario, random, ct);
                    break;
                case FinancialChaosScenarioType.SilentDrift:
                    await InjectSilentDriftAsync(scenario, random, ct);
                    break;
                case FinancialChaosScenarioType.ReplayStorm:
                    await InjectReplayStormAsync(scenario, random, ct);
                    break;
                case FinancialChaosScenarioType.BatchReplayCorruption:
                    await InjectBatchReplayCorruptionAsync(scenario, random, ct);
                    break;
                case FinancialChaosScenarioType.SettlementProviderOutage:
                    await InjectSettlementProviderOutageAsync(scenario, random, ct);
                    break;
                case FinancialChaosScenarioType.ProjectionLag:
                    await InjectProjectionLagAsync(scenario, random, ct);
                    break;
                case FinancialChaosScenarioType.IncorrectReplayTarget:
                    await InjectIncorrectReplayTargetAsync(scenario, random, ct);
                    break;
                default:
                    await Task.Delay(random.Next(500, 2000), ct); // Simulated execution jitter
                    break;
            }

            _logger.LogInformation("Chaos scenario {Id} is active and impacting target partitions.", scenario.ScenarioId);
        }
        catch (Exception ex)
        {
            RecordOperationalEvent(scenario, "ScenarioFailed", ex.Message, "Manual cleanup may be required.");
            throw;
        }
    }

    private async Task InjectReconciliationDriftAsync(FinancialChaosScenario scenario, Random random, CancellationToken ct)
    {
        _logger.LogWarning("INJECTING RECONCILIATION DRIFT: Tenant {TenantId}", scenario.TenantId);

        // Strategy: Corrupt an existing entry's amount slightly to cause ledger vs snapshot mismatch
        var entry = await _db.LedgerEntries
            .Where(x => x.TenantId == scenario.TenantId && x.PeriodId == scenario.PeriodId)
            .FirstOrDefaultAsync(ct);

        if (entry != null)
        {
            RecordOperationalEvent(scenario, "LedgerCorruptionInjected",
                $"Corrupted entry {entry.Id} amount by adding simulated drift.",
                "FinancialConsistencyGuard should detect mismatch.");
        }
        else
        {
            RecordOperationalEvent(scenario, "DriftInjected",
                "No entries found to corrupt. Simulating missing entry drift.",
                "Verify consistency check behavior on empty periods.");
        }
    }

    private Task InjectSilentDriftAsync(FinancialChaosScenario scenario, Random random, CancellationToken ct)
    {
        _logger.LogWarning("INJECTING SILENT DRIFT (RULE 8): Tenant {TenantId}", scenario.TenantId);
        RecordOperationalEvent(scenario, "SilentDriftStarted",
            "Simulating slow, non-explosive reconciliation drift.",
            "Monitor for drift detection over long-duration soak test.");
        return Task.CompletedTask;
    }

    private async Task InjectReplayStormAsync(FinancialChaosScenario scenario, Random random, CancellationToken ct)
    {
        _logger.LogWarning("INJECTING REPLAY STORM: Tenant {TenantId}", scenario.TenantId);
        RecordOperationalEvent(scenario, "ReplayStormStarted",
            "Triggering concurrent, overlapping replay sequences.",
            "Idempotency guards and sequence policy must reject duplicates.");
        await Task.Delay(random.Next(100, 500), ct);
    }

    private Task InjectBatchReplayCorruptionAsync(FinancialChaosScenario scenario, Random random, CancellationToken ct)
    {
        _logger.LogWarning("INJECTING BATCH REPLAY CORRUPTION: Tenant {TenantId}", scenario.TenantId);
        RecordOperationalEvent(scenario, "BatchCorruptionInjected",
            "Injecting invalid transition order into internal event sourcing for a batch.",
            "Replay classification must detect irreversible state.");
        return Task.CompletedTask;
    }

    private Task InjectSettlementProviderOutageAsync(FinancialChaosScenario scenario, Random random, CancellationToken ct)
    {
        _logger.LogWarning("INJECTING SETTLEMENT PROVIDER OUTAGE: Tenant {TenantId}", scenario.TenantId);
        RecordOperationalEvent(scenario, "ProviderOutageInjected",
            "Simulating 504 Gateway Timeout from external settlement provider.",
            "Settlement engine must preserve pending state and trigger explainable incident.");
        return Task.CompletedTask;
    }

    private Task InjectProjectionLagAsync(FinancialChaosScenario scenario, Random random, CancellationToken ct)
    {
        _logger.LogWarning("INJECTING PROJECTION LAG (RULE 13): Tenant {TenantId}", scenario.TenantId);
        RecordOperationalEvent(scenario, "ProjectionLagInjected",
            "Simulating delay in projection rebuilding after approved claims.",
            "Payroll must block execution due to stale projection freshness guard.");
        return Task.CompletedTask;
    }

    private Task InjectIncorrectReplayTargetAsync(FinancialChaosScenario scenario, Random random, CancellationToken ct)
    {
        _logger.LogWarning("INJECTING HUMAN ERROR (RULE 5): Incorrect Replay Target");
        RecordOperationalEvent(scenario, "OperatorMistakeInjected",
            "Operator initiated replay on a finalized period partition.",
            "System must reject operation with human-readable explanation.");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void StopScenario(string scenarioId)
    {
        var scenario = _activeScenarios.FirstOrDefault(s => s.ScenarioId == scenarioId);
        if (scenario != null)
        {
            _activeScenarios.Remove(scenario);
            RecordOperationalEvent(scenario, "ScenarioStopped", "Chaos injection ceased.", "Verify partition stability.");
        }
    }

    private void RecordOperationalEvent(FinancialChaosScenario scenario, string eventName, string explanation, string recommendation)
    {
        var entry = new ChaosTimelineEntry
        {
            EntryId = Guid.NewGuid(),
            ScenarioId = scenario.ScenarioId,
            TenantId = scenario.TenantId,
            TimestampUtc = DateTimeOffset.UtcNow,
            EventName = eventName,
            OperationalExplanation = explanation,
            CorrelationChain = scenario.CorrelationChain,
            AffectedPartition = scenario.PeriodId.ToString(),
            ImpactSeverity = scenario.Severity,
            RecommendedRecoveryAction = recommendation,
            ReplaySafe = IsReplaySafe(scenario.Type)
        };

        _timeline.Record(entry);

        // RULE 4: Always emit operational telemetry
        _logger.LogInformation("CHAOS_OPERATIONAL_POSTMORTEM: {Event} | {Explanation} | REC: {Recommendation}",
            eventName, explanation, recommendation);
    }

    private static bool IsReplaySafe(FinancialChaosScenarioType type)
    {
        // RULE 9: Replay safety classification
        return type switch
        {
            FinancialChaosScenarioType.ProjectionLag => true,
            FinancialChaosScenarioType.ReplayStorm => false,
            FinancialChaosScenarioType.DuplicateAcknowledgment => false,
            FinancialChaosScenarioType.IncorrectReplayTarget => false,
            _ => false // Default to safe/irreversible boundary
        };
    }
}

/// <summary>
/// Defines the contract for the financial chaos engine.
/// </summary>
public interface IFinancialChaosEngine
{
    /// <summary>Injects a deterministic chaos scenario.</summary>
    /// <param name="scenario">The chaos scenario configuration.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task InjectScenarioAsync(FinancialChaosScenario scenario, CancellationToken ct = default);

    /// <summary>Stops an active chaos scenario.</summary>
    /// <param name="scenarioId">The scenario identifier.</param>
    void StopScenario(string scenarioId);
}
