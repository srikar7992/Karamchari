using Karamchari.Core.Domain.Primitives;
using Karamchari.TimeAttendance.Domain.Attendance;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Evaluates an attendance record against its policy and produces exceptions.
/// Called by AttendanceProcessingEngine after each punch finalization.
/// Returns the list of exceptions to be persisted — does not persist itself.
/// </summary>
public sealed class ExceptionEvaluator
{
    /// <summary>
    /// Evaluates a finalized or check-in attendance record and returns exceptions to raise.
    /// </summary>
    public static IReadOnlyList<(AttendanceExceptionType Type, AttendanceExceptionSeverity Severity, string Description)> Evaluate(
        AttendanceRecord record,
        AttendanceShiftPolicy policy,
        GeoPoint? checkInLocation = null,
        GeoPoint? siteCenter = null)
    {
        var exceptions = new List<(AttendanceExceptionType, AttendanceExceptionSeverity, string)>();

        switch (record.Status)
        {
            case AttendanceStatus.Absent:
                exceptions.Add((
                    AttendanceExceptionType.NoShow,
                    AttendanceExceptionSeverity.Critical,
                    $"Employee did not report for shift starting {record.ExpectedStart:yyyy-MM-dd HH:mm}. No check-in recorded."));
                break;

            case AttendanceStatus.Incomplete:
                exceptions.Add((
                    AttendanceExceptionType.MissingCheckOut,
                    AttendanceExceptionSeverity.High,
                    $"Employee checked in at {record.ActualStart:HH:mm} but no check-out recorded before shift end {record.ExpectedEnd:HH:mm}."));
                break;

            case AttendanceStatus.Late:
            case AttendanceStatus.Present:
            case AttendanceStatus.HalfDay:
                EvaluateActiveRecord(record, policy, checkInLocation, siteCenter, exceptions);
                break;
        }

        return exceptions.AsReadOnly();
    }

    private static void EvaluateActiveRecord(
        AttendanceRecord record,
        AttendanceShiftPolicy policy,
        GeoPoint? checkInLocation,
        GeoPoint? siteCenter,
        List<(AttendanceExceptionType, AttendanceExceptionSeverity, string)> exceptions)
    {
        // Late arrival check
        if (record.ActualStart.HasValue)
        {
            int lateMinutes = (int)(record.ActualStart.Value - record.ExpectedStart).TotalMinutes;
            if (lateMinutes > policy.LateToleranceMinutes)
            {
                AttendanceExceptionSeverity severity = policy.ComputeLateArrivalSeverity(lateMinutes);
                exceptions.Add((
                    AttendanceExceptionType.LateArrival,
                    severity,
                    $"Employee arrived {lateMinutes} minutes late. Tolerance: {policy.LateToleranceMinutes} minutes. Expected: {record.ExpectedStart:HH:mm}, Actual: {record.ActualStart.Value:HH:mm}."));
            }
        }
        else
        {
            // Checked in status but no ActualStart — shouldn't happen, defensive
            exceptions.Add((
                AttendanceExceptionType.MissingCheckIn,
                AttendanceExceptionSeverity.High,
                "Check-in time not recorded despite attendance session existing."));
        }

        // Early exit check
        if (record.ActualEnd.HasValue)
        {
            int earlyMinutes = (int)(record.ExpectedEnd - record.ActualEnd.Value).TotalMinutes;
            if (earlyMinutes > policy.EarlyExitToleranceMinutes)
            {
                AttendanceExceptionSeverity severity = policy.ComputeEarlyExitSeverity(earlyMinutes);
                exceptions.Add((
                    AttendanceExceptionType.EarlyExit,
                    severity,
                    $"Employee left {earlyMinutes} minutes early. Tolerance: {policy.EarlyExitToleranceMinutes} minutes. Expected end: {record.ExpectedEnd:HH:mm}, Actual: {record.ActualEnd.Value:HH:mm}."));
            }
        }

        // Overtime check
        if (record.OvertimeMinutes > policy.OvertimeThresholdMinutes)
        {
            exceptions.Add((
                AttendanceExceptionType.Overtime,
                AttendanceExceptionSeverity.Medium,
                $"Employee worked {record.OvertimeMinutes} minutes overtime (threshold: {policy.OvertimeThresholdMinutes} minutes)."));
        }

        // Three-tier geofencing — GPS noise indoors can reach 100m; single threshold causes false violations.
        // AllowedRadius: clean. WarningRadius: logged, no exception. ViolationRadius: exception raised.
        if (policy.EnforceGeofencing && checkInLocation != null && siteCenter != null)
        {
            double distanceMeters = CalculateDistance(checkInLocation, siteCenter);
            if (distanceMeters > policy.GeofenceViolationRadiusMeters)
            {
                exceptions.Add((
                    AttendanceExceptionType.LocationViolation,
                    AttendanceExceptionSeverity.High,
                    $"Employee checked in {distanceMeters:F0}m from site (violation threshold: {policy.GeofenceViolationRadiusMeters}m, warning threshold: {policy.GeofenceWarningRadiusMeters}m)."));
            }
            else if (distanceMeters > policy.GeofenceWarningRadiusMeters)
            {
                exceptions.Add((
                    AttendanceExceptionType.LocationViolation,
                    AttendanceExceptionSeverity.Low,
                    $"Employee checked in {distanceMeters:F0}m from site (outside allowed {policy.GeofenceAllowedRadiusMeters}m, inside violation threshold {policy.GeofenceViolationRadiusMeters}m)."));
            }
            // distanceMeters <= GeofenceWarningRadiusMeters: within warning band, no exception
        }
    }

    private static double CalculateDistance(GeoPoint a, GeoPoint b)
    {
        // Haversine formula
        const double R = 6371000; // meters
        double dLat = ToRad(b.Latitude - a.Latitude);
        double dLon = ToRad(b.Longitude - a.Longitude);
        double sinLat = Math.Sin(dLat / 2);
        double sinLon = Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(
            Math.Sqrt(sinLat * sinLat + Math.Cos(ToRad(a.Latitude)) * Math.Cos(ToRad(b.Latitude)) * sinLon * sinLon),
            Math.Sqrt(1 - (sinLat * sinLat + Math.Cos(ToRad(a.Latitude)) * Math.Cos(ToRad(b.Latitude)) * sinLon * sinLon)));
        return R * c;
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180;
}
