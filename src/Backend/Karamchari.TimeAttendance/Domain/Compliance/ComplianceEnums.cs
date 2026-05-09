namespace Karamchari.TimeAttendance.Domain.Compliance;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum ComplianceRuleType
{
    MaxDailyHours,
    MaxWeeklyHours,
    MinimumDailyRest,
    MinimumWeeklyRest,
    MandatoryBreakInterval,
    OvertimeCeiling,
    NightShiftRestriction
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum ComplianceSeverity
{
    Info,
    Warning,
    Critical,
    Blocker
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record ComplianceResult(bool IsViolated, string? Message, ComplianceSeverity Severity);
