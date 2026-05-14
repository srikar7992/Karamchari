using Karamchari.FinancialOps.Contracts.Primitives;

namespace Karamchari.FinancialOps.Domain.Chaos;

/// <summary>
/// Immutable operational telemetry entry for a chaos event (RULE 4).
/// Provides the postmortem backbone for survivability analysis.
/// </summary>
public sealed record ChaosTimelineEntry
{
    /// <summary>Gets the unique entry identifier.</summary>
    public required Guid EntryId { get; init; }
    /// <summary>Gets the scenario identifier.</summary>
    public required string ScenarioId { get; init; }
    /// <summary>Gets the tenant identifier.</summary>
    public required string TenantId { get; init; }
    /// <summary>Gets the timestamp in UTC.</summary>
    public required DateTimeOffset TimestampUtc { get; init; }
    /// <summary>Gets the name of the events.</summary>
    public required string EventName { get; init; }
    /// <summary>Gets the human-readable operational explanation.</summary>
    public required string OperationalExplanation { get; init; }
    /// <summary>Gets the correlation chain.</summary>
    public required FinancialCorrelationChain CorrelationChain { get; init; }
    /// <summary>Gets the affected partition name (optional).</summary>
    public string? AffectedPartition { get; init; }
    /// <summary>Gets the impact severity.</summary>
    public FinancialChaosSeverity ImpactSeverity { get; init; }
    /// <summary>Gets the recommended recovery action (optional).</summary>
    public string? RecommendedRecoveryAction { get; init; }
    /// <summary>Gets a value indicating whether the scenario is replay-safe.</summary>
    public bool ReplaySafe { get; init; }
}

/// <summary>
/// In-memory or persisted tracker for chaos execution timelines.
/// </summary>
public sealed class ChaosExecutionTimeline
{
    private readonly List<ChaosTimelineEntry> _entries = [];

    /// <summary>Records a new events into the timeline.</summary>
    /// <param name="entry">The timeline entry to record.</param>
    public void Record(ChaosTimelineEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entries.Add(entry);
    }

    /// <summary>Gets all entries in the timeline.</summary>
    /// <returns>A read-only list of all entries.</returns>
    public IReadOnlyList<ChaosTimelineEntry> GetAll() => _entries.OrderBy(e => e.TimestampUtc).ToList();

    /// <summary>Gets all entries for a specific scenario postmortem.</summary>
    /// <param name="scenarioId">The scenario ID.</param>
    /// <returns>A read-only list of entries.</returns>
    public IReadOnlyList<ChaosTimelineEntry> GetForScenario(string scenarioId)
    {
        return _entries.Where(e => e.ScenarioId == scenarioId).OrderBy(e => e.TimestampUtc).ToList();
    }

    /// <summary>Gets metrics for operational trust and decision quality (RULE 7 and RULE 17).</summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>Operational metrics.</returns>
    public ChaosOperationalMetrics GetMetrics(string tenantId)
    {
        var tenantEntries = _entries.Where(e => e.TenantId == tenantId).ToList();
        return new ChaosOperationalMetrics(
            TotalIncidents: tenantEntries.Count(e => e.EventName == "IncidentTriggered"),
            QuarantinesTriggered: tenantEntries.Count(e => e.EventName == "QuarantineApplied"),
            RecoveryActionsProposed: tenantEntries.Count(e => e.RecommendedRecoveryAction != null),
            MeanDecisionDelaySeconds: 0 // Future: track delta between incident and operator action
        );
    }
}

/// <summary>
/// Represents core operational metrics for chaos campaigns.
/// </summary>
/// <param name="TotalIncidents">Total incident count.</param>
/// <param name="QuarantinesTriggered">Total quarantine count.</param>
/// <param name="RecoveryActionsProposed">Total recovery actions.</param>
/// <param name="MeanDecisionDelaySeconds">Average decision latency.</param>
public record ChaosOperationalMetrics(
    int TotalIncidents,
    int QuarantinesTriggered,
    int RecoveryActionsProposed,
    double MeanDecisionDelaySeconds);
