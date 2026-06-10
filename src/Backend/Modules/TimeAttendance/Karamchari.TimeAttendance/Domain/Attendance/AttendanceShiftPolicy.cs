// -----------------------------------------------------------------------
// <copyright file="AttendanceShiftPolicy.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Attendance;

/// <summary>
/// Per-site attendance policy thresholds.
/// Different sites/departments can have different grace periods and overtime rules.
/// The processing engine loads this policy by LocationId when evaluating punches.
/// If no site-specific policy exists, the tenant default (null LocationId) is used.
/// </summary>
public sealed class AttendanceShiftPolicy : AggregateRoot<Guid>, ITenantOwned
{
    private AttendanceShiftPolicy() { }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    /// <summary>Null means this is the tenant-wide default policy.</summary>
    public Guid? LocationId { get; private set; }

    /// <summary>Minutes late before LateArrival exception is raised. Zero = strict.</summary>
    public int LateToleranceMinutes { get; private set; }

    /// <summary>Minutes early before EarlyExit exception is raised.</summary>
    public int EarlyExitToleranceMinutes { get; private set; }

    /// <summary>Maximum missing punches per month before ComplianceViolation is raised.</summary>
    public int MaxMissingPunchesPerMonth { get; private set; }

    /// <summary>Minutes past shift end before Overtime exception is raised.</summary>
    public int OvertimeThresholdMinutes { get; private set; }

    /// <summary>Whether location/GPS check is enforced for check-in.</summary>
    public bool EnforceGeofencing { get; private set; }

    // Three-tier geofencing — GPS noise indoors can be 20-100m, so a single radius causes false violations.
    // AllowedRadius: check-in is clean. WarningRadius: logged but not a violation. ViolationRadius: exception raised.
    public int GeofenceAllowedRadiusMeters { get; private set; }
    public int GeofenceWarningRadiusMeters { get; private set; }
    public int GeofenceViolationRadiusMeters { get; private set; }

    // Severity thresholds — configurable per site/tenant so a factory's 15min late = High
    // while an office's 15min late = Medium. Severity is policy, not domain invariant.
    public int LateArrivalMediumThresholdMinutes { get; private set; }
    public int LateArrivalHighThresholdMinutes { get; private set; }
    public int EarlyExitMediumThresholdMinutes { get; private set; }
    public int EarlyExitHighThresholdMinutes { get; private set; }

    public bool IsDefault { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public AttendanceExceptionSeverity ComputeLateArrivalSeverity(int lateMinutes)
    {
        if (lateMinutes < LateArrivalMediumThresholdMinutes) return AttendanceExceptionSeverity.Low;
        if (lateMinutes < LateArrivalHighThresholdMinutes) return AttendanceExceptionSeverity.Medium;
        return AttendanceExceptionSeverity.High;
    }

    public AttendanceExceptionSeverity ComputeEarlyExitSeverity(int earlyMinutes)
    {
        if (earlyMinutes < EarlyExitMediumThresholdMinutes) return AttendanceExceptionSeverity.Low;
        if (earlyMinutes < EarlyExitHighThresholdMinutes) return AttendanceExceptionSeverity.Medium;
        return AttendanceExceptionSeverity.High;
    }

    public static AttendanceShiftPolicy CreateDefault(string tenantId)
    {
        return new AttendanceShiftPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Default Policy",
            LocationId = null,
            LateToleranceMinutes = 5,
            EarlyExitToleranceMinutes = 5,
            MaxMissingPunchesPerMonth = 3,
            OvertimeThresholdMinutes = 30,
            EnforceGeofencing = false,
            GeofenceAllowedRadiusMeters = 100,
            GeofenceWarningRadiusMeters = 200,
            GeofenceViolationRadiusMeters = 500,
            LateArrivalMediumThresholdMinutes = 10,
            LateArrivalHighThresholdMinutes = 30,
            EarlyExitMediumThresholdMinutes = 10,
            EarlyExitHighThresholdMinutes = 30,
            IsDefault = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public static AttendanceShiftPolicy Create(
        string tenantId,
        string name,
        Guid locationId,
        int lateToleranceMinutes = 5,
        int earlyExitToleranceMinutes = 5,
        int maxMissingPunchesPerMonth = 3,
        int overtimeThresholdMinutes = 30,
        bool enforceGeofencing = false,
        int geofenceAllowedRadiusMeters = 100,
        int geofenceWarningRadiusMeters = 200,
        int geofenceViolationRadiusMeters = 500,
        int lateArrivalMediumThresholdMinutes = 10,
        int lateArrivalHighThresholdMinutes = 30,
        int earlyExitMediumThresholdMinutes = 10,
        int earlyExitHighThresholdMinutes = 30)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lateToleranceMinutes);
        ArgumentOutOfRangeException.ThrowIfNegative(earlyExitToleranceMinutes);
        ArgumentOutOfRangeException.ThrowIfNegative(maxMissingPunchesPerMonth);
        ArgumentOutOfRangeException.ThrowIfNegative(overtimeThresholdMinutes);

        return new AttendanceShiftPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            LocationId = locationId,
            LateToleranceMinutes = lateToleranceMinutes,
            EarlyExitToleranceMinutes = earlyExitToleranceMinutes,
            MaxMissingPunchesPerMonth = maxMissingPunchesPerMonth,
            OvertimeThresholdMinutes = overtimeThresholdMinutes,
            EnforceGeofencing = enforceGeofencing,
            GeofenceAllowedRadiusMeters = geofenceAllowedRadiusMeters,
            GeofenceWarningRadiusMeters = geofenceWarningRadiusMeters,
            GeofenceViolationRadiusMeters = geofenceViolationRadiusMeters,
            LateArrivalMediumThresholdMinutes = lateArrivalMediumThresholdMinutes,
            LateArrivalHighThresholdMinutes = lateArrivalHighThresholdMinutes,
            EarlyExitMediumThresholdMinutes = earlyExitMediumThresholdMinutes,
            EarlyExitHighThresholdMinutes = earlyExitHighThresholdMinutes,
            IsDefault = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Update(
        int lateToleranceMinutes,
        int earlyExitToleranceMinutes,
        int maxMissingPunchesPerMonth,
        int overtimeThresholdMinutes,
        bool enforceGeofencing,
        int geofenceAllowedRadiusMeters,
        int geofenceWarningRadiusMeters,
        int geofenceViolationRadiusMeters,
        int lateArrivalMediumThresholdMinutes = 10,
        int lateArrivalHighThresholdMinutes = 30,
        int earlyExitMediumThresholdMinutes = 10,
        int earlyExitHighThresholdMinutes = 30)
    {
        LateToleranceMinutes = lateToleranceMinutes;
        EarlyExitToleranceMinutes = earlyExitToleranceMinutes;
        MaxMissingPunchesPerMonth = maxMissingPunchesPerMonth;
        OvertimeThresholdMinutes = overtimeThresholdMinutes;
        EnforceGeofencing = enforceGeofencing;
        GeofenceAllowedRadiusMeters = geofenceAllowedRadiusMeters;
        GeofenceWarningRadiusMeters = geofenceWarningRadiusMeters;
        GeofenceViolationRadiusMeters = geofenceViolationRadiusMeters;
        LateArrivalMediumThresholdMinutes = lateArrivalMediumThresholdMinutes;
        LateArrivalHighThresholdMinutes = lateArrivalHighThresholdMinutes;
        EarlyExitMediumThresholdMinutes = earlyExitMediumThresholdMinutes;
        EarlyExitHighThresholdMinutes = earlyExitHighThresholdMinutes;
    }
}
