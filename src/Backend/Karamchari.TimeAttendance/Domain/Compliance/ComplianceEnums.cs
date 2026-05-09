namespace Karamchari.TimeAttendance.Domain.Compliance;

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

public enum ComplianceSeverity
{
    Info,
    Warning,
    Critical,
    Blocker
}

public sealed record ComplianceResult(bool IsViolated, string? Message, ComplianceSeverity Severity);
