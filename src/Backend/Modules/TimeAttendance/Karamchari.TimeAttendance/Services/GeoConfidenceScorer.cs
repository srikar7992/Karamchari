// -----------------------------------------------------------------------
// <copyright file="GeoConfidenceScorer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.TimeAttendance.Domain.Biometric;
using Karamchari.TimeAttendance.Domain.Geo;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Fuses GPS, WiFi, IP whitelist, BLE, and VPN signals into a single geo confidence score.
/// Also runs fraud detection: impossible travel, mock GPS, device cloning, hard/soft zone violations.
/// </summary>
public sealed class GeoConfidenceScorer(TimeAttendanceDbContext db, ILogger<GeoConfidenceScorer> logger)
{
    // Impossible travel defaults: 50 km in 30 minutes
    private const double DefaultImpossibleTravelKm = 50.0;
    private const int DefaultImpossibleTravelMinutes = 30;

    private readonly TimeAttendanceDbContext _db = db;
    private readonly ILogger<GeoConfidenceScorer> _logger = logger;

    public sealed record GeoScoringRequest(
        string TenantId,
        Guid EmployeeId,
        double? Latitude,
        double? Longitude,
        double? GpsAccuracyMeters,
        bool WifiSsidMatched,
        bool IpWhitelisted,
        bool BleBeaconDetected,
        bool VpnMatched,
        bool MockGpsDetected,
        bool JailbreakDetected,
        string? DeviceFingerprint,
        DateTimeOffset PunchTimeUtc);

    public sealed record GeoScoringResult(
        GeoSignalFusion Fusion,
        Guid? MatchedZoneId,
        string? MatchedZoneName,
        IReadOnlyList<GeoFraudSignal> FraudSignals,
        bool ShouldBlock);

    public async Task<GeoScoringResult> ScoreAsync(
        GeoScoringRequest request,
        CancellationToken ct = default)
    {
        var fraudSignals = new List<GeoFraudSignal>();

        // Hard blocks — evaluate before zone check
        if (request.JailbreakDetected)
        {
            fraudSignals.Add(GeoFraudSignal.Jailbreak());
        }

        if (request.MockGpsDetected)
        {
            fraudSignals.Add(GeoFraudSignal.MockGps());
        }

        // Impossible travel check
        GeoFraudSignal? travelFraud = await CheckImpossibleTravelAsync(request, ct);
        if (travelFraud is not null)
            fraudSignals.Add(travelFraud);

        // Device cloning check
        GeoFraudSignal? cloningFraud = await CheckDeviceCloningAsync(request, ct);
        if (cloningFraud is not null)
            fraudSignals.Add(cloningFraud);

        // Zone matching
        Guid? matchedZoneId = null;
        string? matchedZoneName = null;
        bool gpsInsideZone = false;

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            (Guid? zoneId, string? zoneName, bool inside) = await FindMatchingZoneAsync(
                request.TenantId, request.Latitude.Value, request.Longitude.Value, ct);

            matchedZoneId = zoneId;
            matchedZoneName = zoneName;
            gpsInsideZone = inside;

            if (!inside && zoneId is null)
            {
                fraudSignals.Add(GeoFraudSignal.HardZoneViolation("any valid zone"));
            }
            else if (!inside && zoneId is not null)
            {
                fraudSignals.Add(GeoFraudSignal.SoftZoneViolation(zoneName!));
            }
        }

        var fusion = GeoSignalFusion.Compute(
            gpsInsideZone: gpsInsideZone,
            gpsAccuracyMeters: request.GpsAccuracyMeters,
            wifiMatched: request.WifiSsidMatched,
            ipWhitelisted: request.IpWhitelisted,
            bleBeaconDetected: request.BleBeaconDetected,
            vpnMatched: request.VpnMatched);

        bool highSeverityFraud = fraudSignals.Any(f => f.Severity == FraudAlertSeverity.High);
        bool shouldBlock = highSeverityFraud || fusion.IsRejected;

        _logger.LogDebug(
            "GeoScore employee={EmployeeId} score={Score} outcome={Outcome} zone={Zone} fraudCount={Fraud}",
            request.EmployeeId, fusion.TotalScore, fusion.Outcome, matchedZoneName, fraudSignals.Count);

        return new GeoScoringResult(fusion, matchedZoneId, matchedZoneName, fraudSignals, shouldBlock);
    }

    private async Task<GeoFraudSignal?> CheckImpossibleTravelAsync(
        GeoScoringRequest request, CancellationToken ct)
    {
        if (!request.Latitude.HasValue || !request.Longitude.HasValue)
            return null;

        // Last accepted punch for this employee
        PunchEvent? lastPunch = await _db.PunchEvents
            .Where(p => p.TenantId == request.TenantId
                     && p.EmployeeId == request.EmployeeId
                     && p.RejectionReason == null
                     && p.Latitude != null
                     && p.Longitude != null)
            .OrderByDescending(p => p.ReceivedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (lastPunch?.Latitude is null || lastPunch.Longitude is null)
            return null;

        double distanceKm = HaversineKm(
            lastPunch.Latitude.Value, lastPunch.Longitude.Value,
            request.Latitude.Value, request.Longitude.Value);

        int elapsedMinutes = (int)(request.PunchTimeUtc - lastPunch.ReceivedAtUtc).TotalMinutes;

        if (elapsedMinutes < DefaultImpossibleTravelMinutes && distanceKm > DefaultImpossibleTravelKm)
            return GeoFraudSignal.ImpossibleTravel(distanceKm, elapsedMinutes);

        return null;
    }

    private static async Task<GeoFraudSignal?> CheckDeviceCloningAsync(
        GeoScoringRequest request, CancellationToken ct)
    {
        if (request.DeviceFingerprint is null) return null;

        // Check for same device fingerprint from a different IP in the last 5 minutes
        // In practice device fingerprint is on PunchEvent via DeviceId; simplified here.
        // Full implementation would cross-reference DeviceId → network IP log.
        await Task.CompletedTask; // async placeholder
        return null;
    }

    private async Task<(Guid? ZoneId, string? ZoneName, bool Inside)> FindMatchingZoneAsync(
        string tenantId, double latitude, double longitude, CancellationToken ct)
    {
        List<GeoZone> zones = await _db.GeoZones
            .Where(z => z.TenantId == tenantId && z.IsActive && z.ZoneType == GeoZoneType.Radial)
            .OrderBy(z => z.ApproximateAreaSquareMeters) // smallest first = most specific
            .ToListAsync(ct);

        foreach (GeoZone? zone in zones)
        {
            if (zone.ContainsPoint(latitude, longitude))
                return (zone.Id, zone.Name, true);
        }

        // No match — check if any zone is nearby (soft violation: within 2x radius)
        foreach (GeoZone? zone in zones)
        {
            if (zone.CenterLatitude.HasValue && zone.CenterLongitude.HasValue && zone.RadiusMeters.HasValue)
            {
                double dist = HaversineMeters(
                    zone.CenterLatitude.Value, zone.CenterLongitude.Value, latitude, longitude);

                // Within 2x radius = soft violation, not hard
                if (dist <= zone.RadiusMeters.Value * 2.0)
                    return (zone.Id, zone.Name, false);
            }
        }

        return (null, null, false);
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        => HaversineMeters(lat1, lon1, lat2, lon2) / 1000.0;

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        double dLat = ToRad(lat2 - lat1);
        double dLon = ToRad(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
}
