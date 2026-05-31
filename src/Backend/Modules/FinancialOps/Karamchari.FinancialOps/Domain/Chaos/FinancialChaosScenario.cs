using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Domain.Periods;

namespace Karamchari.FinancialOps.Domain.Chaos;

/// <summary>
/// Defines the categories of financial chaos scenarios for the elite survivability suite.
/// Strictly domain-focused per RULE 1.
/// </summary>
public enum FinancialChaosScenarioType
{
    /// <summary>Simulate an outage from the external settlement provider.</summary>
    SettlementProviderOutage = 1,
    /// <summary>Simulate a storm of replay requests for the same correlation root.</summary>
    ReplayStorm = 2,
    /// <summary>Simulate lag in projection freshness.</summary>
    ProjectionLag = 3,
    /// <summary>Simulate a duplicate acknowledgment from a provider.</summary>
    DuplicateAcknowledgment = 4,
    /// <summary>Simulate drift between the ledger and finalization snapshots.</summary>
    ReconciliationDrift = 5,
    /// <summary>Simulate corruption in the payroll projections.</summary>
    PayrollProjectionCorruption = 6,
    /// <summary>Simulate a worker crash during financial processing.</summary>
    WorkerCrash = 7,
    /// <summary>Simulate invalid replay transitions for a batch.</summary>
    BatchReplayCorruption = 8,
    /// <summary>Simulate concurrent recovery attempts for a settlement.</summary>
    ConcurrentSettlementRecovery = 9,
    /// <summary>Simulate resource exhaustion (e.g. CPU, memory, connection pool).</summary>
    ResourceExhaustion = 10,
    /// <summary>Simulate temporal skew (clock skew, delayed timestamps).</summary>
    TemporalSkew = 11,
    /// <summary>Simulate slow, silent reconciliation drift over time.</summary>
    SilentDrift = 12,

    /// <summary>Operator mistake: Replay initiated on the wrong partition.</summary>
    IncorrectReplayTarget = 101,
    /// <summary>Operator mistake: Execution of a stale recovery guidance.</summary>
    StaleRecoveryExecution = 102,
    /// <summary>Operator mistake: Releasing a quarantine prematurely.</summary>
    QuarantineReleaseError = 103,
    /// <summary>Operator mistake: Concurrent execution of compensation.</summary>
    ConcurrentCompensationExecution = 104,
    /// <summary>Operator mistake: Executing a duplicate manual override.</summary>
    DuplicateOverrideExecution = 105,
    /// <summary>Operator mistake: Re-running a settlement that was already successful.</summary>
    WrongSettlementRerun = 106,
    /// <summary>Operator mistake: Triggering a storm of retries due to panic.</summary>
    OperatorPanicStorm = 107,
    /// <summary>Operator mistake: Following incorrect or conflicting recovery guidance.</summary>
    BadRecoveryGuidance = 108
}

/// <summary>
/// Represents the severity of a chaos scenario and its expected operational impact.
/// </summary>
public enum FinancialChaosSeverity
{
    /// <summary>Minor operational friction, no immediate risk.</summary>
    Degraded,
    /// <summary>Localized partition failure, quarantine expected.</summary>
    PartitionFailure,
    /// <summary>High risk of financial drift if not recovered.</summary>
    Critical,
    /// <summary>Extreme stress test, multiple cascading failures.</summary>
    Elite
}

/// <summary>
/// Deterministic configuration for a chaos scenario (RULE 2).
/// Supports partition-scoped isolation (RULE 3).
/// </summary>
public sealed record FinancialChaosScenario
{
    /// <summary>Gets the unique scenario identifier.</summary>
    public required string ScenarioId { get; init; }
    /// <summary>Gets the type of chaos scenario.</summary>
    public required FinancialChaosScenarioType Type { get; init; }
    /// <summary>Gets the impact severity.</summary>
    public required FinancialChaosSeverity Severity { get; init; }
    /// <summary>Gets the target tenant identifier.</summary>
    public required string TenantId { get; init; }
    /// <summary>Gets the target operational period.</summary>
    public required FinancialOperationalPeriodId PeriodId { get; init; }
    /// <summary>Gets the deterministic seed for reproducibility.</summary>
    public required string DeterministicSeed { get; init; }
    /// <summary>Gets the operational correlation chain.</summary>
    public required FinancialCorrelationChain CorrelationChain { get; init; }
    /// <summary>Gets the scenario duration.</summary>
    public TimeSpan Duration { get; init; } = TimeSpan.FromMinutes(5);
    /// <summary>Gets specific parameters for the scenario.</summary>
    public Dictionary<string, string> Parameters { get; init; } = [];

    /// <summary>Creates a deterministic scenario for a specific partition.</summary>
    /// <param name="type">Scenario type.</param>
    /// <param name="tenantId">Tenant ID.</param>
    /// <param name="periodId">Period ID.</param>
    /// <param name="seed">Deterministic seed.</param>
    /// <param name="severity">Impact severity.</param>
    /// <returns>A new chaos scenario configuration.</returns>
    public static FinancialChaosScenario Create(
        FinancialChaosScenarioType type,
        string tenantId,
        FinancialOperationalPeriodId periodId,
        string seed,
        FinancialChaosSeverity severity = FinancialChaosSeverity.PartitionFailure)
    {
        return new FinancialChaosScenario
        {
            ScenarioId = $"CHAOS-{type}-{tenantId}-{DateTimeOffset.UtcNow.Ticks}",
            Type = type,
            Severity = severity,
            TenantId = tenantId,
            PeriodId = periodId,
            DeterministicSeed = seed,
            CorrelationChain = FinancialCorrelationChain.CreateNew()
        };
    }
}
