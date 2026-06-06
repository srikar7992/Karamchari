using Karamchari.Intelligence.Domain.Workforce;

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Result of causal chain detection for one employee.
/// </summary>
/// <param name="ActiveLinks">Ordered list of active chain links (causal sequence order).</param>
/// <param name="ChainSeverity">Severity derived from the count of active links.</param>
public sealed record CausalChainResult(
    IReadOnlyList<CausalChainLink> ActiveLinks,
    CausalChainSeverity ChainSeverity
);

/// <summary>
/// Detects which links in the workforce risk propagation chain are active for a single employee.
///
/// Chain direction (typical causal sequence):
///   CoverageShortage → OvertimeIncrease → LeaveAvoidance → ScheduleDegradation → BurnoutRisk → AttritionRisk
///
/// Thresholds:
///   CoverageShortage    : CriticalShiftCoverageRatio ≥ 0.50
///   OvertimeIncrease    : OvertimeHours28d           ≥ 24h
///   LeaveAvoidance      : DaysWithoutLeave           ≥ 60 days
///   ScheduleDegradation : ScheduleQualityScore       ≤ 60 (lower = worse quality)
///   BurnoutRisk         : BurnoutScore               ≥ 60
///   AttritionRisk       : AttritionScore             ≥ 60
///
/// Links are detected independently (not enforced to fire in sequence).
/// "Active" means the threshold is breached today — not that the causal arrow was observed.
/// Time-series causal attribution is Phase 6.5+ work.
///
/// Zero I/O. Pure static.
/// </summary>
public static class CausalChainDetector
{
    private const decimal CoverageShortageThreshold = 0.50m;
    private const decimal OvertimeIncreaseThreshold = 24m;
    private const int LeaveAvoidanceThreshold = 60;
    private const decimal ScheduleDegradationThreshold = 60m;
    private const decimal BurnoutRiskThreshold = 60m;
    private const decimal AttritionRiskThreshold = 60m;

    /// <summary>
    /// Detects active causal chain links for the given employee inputs.
    /// </summary>
    /// <param name="input">Employee-level scores and signals. Must not be null.</param>
    /// <returns>Active links in causal sequence order and overall severity.</returns>
    public static CausalChainResult Detect(CausalChainInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var active = new List<CausalChainLink>();

        if (input.CriticalShiftCoverageRatio >= CoverageShortageThreshold)
            active.Add(CausalChainLink.CoverageShortage);

        if (input.OvertimeHours28d >= OvertimeIncreaseThreshold)
            active.Add(CausalChainLink.OvertimeIncrease);

        if (input.DaysWithoutLeave >= LeaveAvoidanceThreshold)
            active.Add(CausalChainLink.LeaveAvoidance);

        // ScheduleQualityScore of -1 means data not available — skip
        if (input.ScheduleQualityScore >= 0m && input.ScheduleQualityScore <= ScheduleDegradationThreshold)
            active.Add(CausalChainLink.ScheduleDegradation);

        if (input.BurnoutScore >= BurnoutRiskThreshold)
            active.Add(CausalChainLink.BurnoutRisk);

        if (input.AttritionScore >= AttritionRiskThreshold)
            active.Add(CausalChainLink.AttritionRisk);

        var severity = active.Count switch
        {
            0 => CausalChainSeverity.None,
            <= 2 => CausalChainSeverity.Mild,
            <= 4 => CausalChainSeverity.Moderate,
            _ => CausalChainSeverity.Severe
        };

        return new CausalChainResult(active, severity);
    }
}
