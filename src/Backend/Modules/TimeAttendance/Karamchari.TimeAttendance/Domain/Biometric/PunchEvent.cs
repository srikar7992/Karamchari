// -----------------------------------------------------------------------
// <copyright file="PunchEvent.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Biometric;

/// <summary>
/// Immutable, append-only event store entry. The source of truth for every punch.
/// AttendanceSession and AttendanceRecord are derived projections.
/// One row per physical punch (dedup prevents duplicates within the configurable window).
/// </summary>
public sealed class PunchEvent : Entity<Guid>, ITenantOwned
{
    private PunchEvent() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }

    /// <summary>Device that produced the punch. Null for web/mobile soft punches.</summary>
    public Guid? DeviceId { get; private set; }

    /// <summary>Server-assigned UTC timestamp. Authoritative; device clock is advisory.</summary>
    public DateTimeOffset ReceivedAtUtc { get; private set; }

    /// <summary>Device-reported UTC timestamp. May drift; flagged when skew > threshold.</summary>
    public DateTimeOffset DeviceTimestampUtc { get; private set; }

    public PunchDirection Direction { get; private set; }
    public CaptureMode CaptureMode { get; private set; }
    public BiometricModality? Modality { get; private set; }

    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public double? GpsAccuracyMeters { get; private set; }

    /// <summary>Geo confidence score 0-100 computed by GeoConfidenceScorer.</summary>
    public int GeoConfidenceScore { get; private set; }

    /// <summary>Biometric match score returned by the matching engine (0.0–1.0). Null for non-biometric.</summary>
    public double? BiometricMatchScore { get; private set; }

    /// <summary>Idempotency key from device. Used for dedup within the configured window.</summary>
    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>True when device clock skew exceeded the configured threshold.</summary>
    public bool ClockSkewFlagged { get; private set; }

    /// <summary>True when geo-validation failed but punch was accepted (soft flag).</summary>
    public bool GeoSoftFlagged { get; private set; }

    /// <summary>Null = accepted; non-null = rejected with reason.</summary>
    public string? RejectionReason { get; private set; }

    public bool IsAccepted => RejectionReason is null;

    public static PunchEvent Record(
        string tenantId,
        Guid employeeId,
        Guid? deviceId,
        DateTimeOffset deviceTimestampUtc,
        PunchDirection direction,
        CaptureMode captureMode,
        BiometricModality? modality,
        double? latitude,
        double? longitude,
        double? gpsAccuracyMeters,
        int geoConfidenceScore,
        double? biometricMatchScore,
        string idempotencyKey,
        bool clockSkewFlagged = false,
        bool geoSoftFlagged = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        return new PunchEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            DeviceId = deviceId,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
            DeviceTimestampUtc = deviceTimestampUtc,
            Direction = direction,
            CaptureMode = captureMode,
            Modality = modality,
            Latitude = latitude,
            Longitude = longitude,
            GpsAccuracyMeters = gpsAccuracyMeters,
            GeoConfidenceScore = Math.Clamp(geoConfidenceScore, 0, 100),
            BiometricMatchScore = biometricMatchScore,
            IdempotencyKey = idempotencyKey,
            ClockSkewFlagged = clockSkewFlagged,
            GeoSoftFlagged = geoSoftFlagged,
        };
    }

    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        RejectionReason = reason;
    }

    /// <summary>
    /// Returns the authoritative timestamp to use for attendance processing.
    /// Server time is preferred; device time used only when server time unavailable.
    /// </summary>
    public DateTimeOffset AuthoritativeTimestamp => ReceivedAtUtc;
}
