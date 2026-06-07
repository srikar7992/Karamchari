namespace Karamchari.TimeAttendance.Domain.Geo;

public enum GeoFraudType
{
    ImpossibleTravel,
    MockGpsDetected,
    DeviceCloning,
    JailbreakRooted,
    ZoneViolationHard,
    ZoneViolationSoft
}

public enum FraudAlertSeverity
{
    Low,
    Medium,
    High
}

/// <summary>
/// Value object describing a fraud signal detected during geo validation.
/// High severity = immediate block. Medium = quarantine + manager notification. Low = weekly digest.
/// </summary>
public sealed record GeoFraudSignal(
    GeoFraudType Type,
    FraudAlertSeverity Severity,
    string Description)
{
    public static GeoFraudSignal ImpossibleTravel(double distanceKm, int elapsedMinutes)
        => new(
            GeoFraudType.ImpossibleTravel,
            FraudAlertSeverity.High,
            $"Impossible travel: {distanceKm:F1} km in {elapsedMinutes} minutes.");

    public static GeoFraudSignal MockGps()
        => new(GeoFraudType.MockGpsDetected, FraudAlertSeverity.Medium, "Mock GPS provider detected on device.");

    public static GeoFraudSignal DeviceCloning(string deviceFingerprint)
        => new(GeoFraudType.DeviceCloning, FraudAlertSeverity.High, $"Device fingerprint {deviceFingerprint} seen from two IPs simultaneously.");

    public static GeoFraudSignal Jailbreak()
        => new(GeoFraudType.JailbreakRooted, FraudAlertSeverity.High, "Device jailbreak/root detected.");

    public static GeoFraudSignal HardZoneViolation(string zoneName)
        => new(GeoFraudType.ZoneViolationHard, FraudAlertSeverity.Medium, $"Punch far outside valid zone '{zoneName}'.");

    public static GeoFraudSignal SoftZoneViolation(string zoneName)
        => new(GeoFraudType.ZoneViolationSoft, FraudAlertSeverity.Low, $"Punch slightly outside zone '{zoneName}' boundary.");
}
