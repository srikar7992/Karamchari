// -----------------------------------------------------------------------
// <copyright file="GeoSignalFusion.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Geo;

/// <summary>
/// Value object representing the result of fusing multiple location signals into a single confidence score.
/// Score 0-100. Thresholds: 70+ = Accepted, 40-69 = SoftFlag, below 40 = Rejected.
/// </summary>
public sealed record GeoSignalFusion
{
    public int GpsPoints { get; init; }
    public int WifiPoints { get; init; }
    public int IpWhitelistPoints { get; init; }
    public int BleBeaconPoints { get; init; }
    public int VpnPoints { get; init; }

    public int TotalScore => Math.Clamp(
        GpsPoints + WifiPoints + IpWhitelistPoints + BleBeaconPoints + VpnPoints,
        0, 100);

    public GeoValidationOutcome Outcome => TotalScore switch
    {
        >= 70 => GeoValidationOutcome.Accepted,
        >= 40 => GeoValidationOutcome.SoftFlag,
        _ => GeoValidationOutcome.Rejected,
    };

    public bool IsAccepted => Outcome == GeoValidationOutcome.Accepted;
    public bool IsSoftFlagged => Outcome == GeoValidationOutcome.SoftFlag;
    public bool IsRejected => Outcome == GeoValidationOutcome.Rejected;

    public static GeoSignalFusion Compute(
        bool gpsInsideZone,
        double? gpsAccuracyMeters,
        bool wifiMatched,
        bool ipWhitelisted,
        bool bleBeaconDetected,
        bool vpnMatched)
    {
        // GPS: up to 35 points. Score decreases with accuracy degradation (higher accuracy value = worse).
        int gpsPoints = 0;
        if (gpsInsideZone)
        {
            gpsPoints = gpsAccuracyMeters switch
            {
                <= 10 => 35,
                <= 25 => 30,
                <= 50 => 25,
                <= 100 => 20,
                _ => 10,
            };
        }

        return new GeoSignalFusion
        {
            GpsPoints = gpsPoints,
            WifiPoints = wifiMatched ? 25 : 0,
            IpWhitelistPoints = ipWhitelisted ? 20 : 0,
            BleBeaconPoints = bleBeaconDetected ? 15 : 0,
            VpnPoints = vpnMatched ? 5 : 0,
        };
    }

    /// <summary>No location signals available at all — minimum score.</summary>
    public static GeoSignalFusion NoSignal() => new();
}

public enum GeoValidationOutcome
{
    Accepted,
    SoftFlag,
    Rejected
}
