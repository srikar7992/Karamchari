namespace Karamchari.TimeAttendance.Domain.Rostering;

/// <summary>
/// Alert types for published roster state — NOT for constraint violations.
/// Hard constraints (leave, skill, availability) block assignment at write time, so they can never
/// appear in a published roster. Alerts should represent observable risks, not impossible states.
/// </summary>
public enum ExceptionType
{
    /// <summary>Shift headcount will drop below minimum if any current assignee cancels.</summary>
    CoverageRisk = 1,

    /// <summary>Employee is approaching the maximum hours threshold for the current measurement window.</summary>
    NearOvertime = 2,

    /// <summary>Location has fewer assignments than its MinimumStaff requirement on a given date.</summary>
    UnderstaffedLocation = 3,

    /// <summary>An employee's skill certification expires before the shift date.</summary>
    CertificationExpiry = 4,

    /// <summary>Rest period between consecutive shifts is shorter than policy minimum.</summary>
    RestViolation = 5,

    /// <summary>Employee has exceeded or is at the maximum hours threshold.</summary>
    OvertimeRisk = 6,
}

public enum ExceptionSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3,
}
