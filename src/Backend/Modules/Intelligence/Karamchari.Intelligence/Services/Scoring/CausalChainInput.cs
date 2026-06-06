namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Inputs for <see cref="CausalChainDetector"/>.
/// Each field maps to an existing Intelligence signal or score for a single employee.
/// </summary>
/// <param name="CriticalShiftCoverageRatio">Fraction of critical shifts only this employee can fill [0–1]. Source: WorkforceSignalType.CriticalShiftCoverageRatio.</param>
/// <param name="OvertimeHours28d">Overtime hours in rolling 28-day window. Source: WorkforceSignalType.OvertimeHours28d.</param>
/// <param name="DaysWithoutLeave">Days elapsed since last approved leave. Source: WorkforceSignalType.DaysWithoutLeave.</param>
/// <param name="ScheduleQualityScore">Schedule ergonomic quality score [0–100]. Source: EmployeeScheduleQuality.QualityScore. Pass -1 if not available.</param>
/// <param name="BurnoutScore">Latest burnout score [0–100]. Source: WorkforceBurnoutScore.Score.</param>
/// <param name="AttritionScore">Latest attrition risk score [0–100]. Source: WorkforceAttritionScore.Score.</param>
public sealed record CausalChainInput(
    decimal CriticalShiftCoverageRatio,
    decimal OvertimeHours28d,
    int DaysWithoutLeave,
    decimal ScheduleQualityScore,
    decimal BurnoutScore,
    decimal AttritionScore
);
