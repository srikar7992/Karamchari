namespace Karamchari.TimeAttendance.Domain.Attendance;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum AttendanceStatus
{
    Scheduled,
    CheckedIn,
    OnBreak,
    ReturnedFromBreak,
    CheckedOut,
    Missed,
    Corrected,
    Disputed
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum AttendanceSource
{
    WebPortal,
    MobileApp,
    Kiosk,
    BiometricDevice,
    BulkImport,
    SystemAutoAction
}
