namespace Karamchari.TimeAttendance.Domain.Biometric;

public enum BiometricModality
{
    Fingerprint,
    Face,
    Iris,
    PalmVein,
    SmartCard,
    Pin
}

public enum BiometricMatchMode
{
    OnServer,
    OnDevice
}

public enum DeviceStatus
{
    Active,
    Offline,
    Tampered,
    Decommissioned,
    PendingRegistration
}

public enum PunchDirection
{
    In,
    Out
}

public enum CaptureMode
{
    Biometric,
    Geo,
    Mobile,
    WebPortal,
    SmartCard,
    Manual
}
