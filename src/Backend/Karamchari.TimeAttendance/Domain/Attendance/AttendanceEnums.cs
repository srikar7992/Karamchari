namespace Karamchari.TimeAttendance.Domain.Attendance;

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

public enum AttendanceSource
{
    WebPortal,
    MobileApp,
    Kiosk,
    BiometricDevice,
    BulkImport,
    SystemAutoAction
}
