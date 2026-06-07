using Karamchari.Core.Domain.Primitives;
using Karamchari.TimeAttendance.Contracts;
using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Domain.Biometric;
using Karamchari.TimeAttendance.Domain.Geo;
using Karamchari.TimeAttendance.Domain.Policy;
using Karamchari.TimeAttendance.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Entry point for all punch events (biometric, mobile, web, card, manual).
/// Responsibilities:
///   1. Deduplication within the configured window (idempotency key + employee + window).
///   2. Device clock skew validation.
///   3. Geo confidence scoring + fraud detection.
///   4. PunchEvent persistence (immutable event store).
///   5. Forward to AttendanceProcessingEngine for session/record updates.
/// </summary>
public sealed class PunchIngestionService(
    TimeAttendanceDbContext db,
    GeoConfidenceScorer geoScorer,
    AttendancePolicyHierarchyResolver policyResolver,
    AttendanceProcessingEngine processingEngine,
    IPublishEndpoint bus,
    ILogger<PunchIngestionService> logger)
{
    private readonly TimeAttendanceDbContext _db = db;
    private readonly GeoConfidenceScorer _geoScorer = geoScorer;
    private readonly AttendancePolicyHierarchyResolver _policyResolver = policyResolver;
    private readonly AttendanceProcessingEngine _processingEngine = processingEngine;
    private readonly IPublishEndpoint _bus = bus;
    private readonly ILogger<PunchIngestionService> _logger = logger;

    public sealed record PunchRequest(
        string TenantId,
        Guid EmployeeId,
        Guid? DeviceId,
        DateTimeOffset DeviceTimestampUtc,
        PunchDirection Direction,
        CaptureMode CaptureMode,
        BiometricModality? Modality,
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
        double? BiometricMatchScore,
        string IdempotencyKey);

    public sealed record PunchResult(
        bool Accepted,
        string? RejectionReason,
        Guid? PunchEventId,
        int GeoConfidenceScore,
        GeoValidationOutcome GeoOutcome,
        IReadOnlyList<GeoFraudSignal> FraudSignals);

    public async Task<PunchResult> IngestAsync(PunchRequest request, CancellationToken ct = default)
    {
        AttendancePolicyHierarchy policy = await _policyResolver.ResolveForEmployeeAsync(request.TenantId, request.EmployeeId, ct: ct);

        // Step 1: Deduplication
        DateTimeOffset dedupWindowStart = DateTimeOffset.UtcNow.AddSeconds(-policy.PunchDedupWindowSeconds);
        bool isDuplicate = await _db.PunchEvents
            .AnyAsync(p =>
                p.TenantId == request.TenantId &&
                p.EmployeeId == request.EmployeeId &&
                p.IdempotencyKey == request.IdempotencyKey &&
                p.ReceivedAtUtc >= dedupWindowStart,
                ct);

        if (isDuplicate)
        {
            _logger.LogDebug(
                "Duplicate punch dropped. Employee={EmployeeId} Key={Key}",
                request.EmployeeId, request.IdempotencyKey);
            return new PunchResult(false, "Duplicate punch within dedup window.", null, 0, GeoValidationOutcome.Accepted, []);
        }

        // Step 2: Clock skew check
        DateTimeOffset serverNow = DateTimeOffset.UtcNow;
        double skewMinutes = Math.Abs((serverNow - request.DeviceTimestampUtc).TotalMinutes);
        bool clockSkewFlagged = skewMinutes > policy.MaxClockSkewMinutes;

        if (clockSkewFlagged)
        {
            _logger.LogWarning(
                "Clock skew {SkewMinutes:F1}min for employee {EmployeeId}. Punch accepted but flagged.",
                skewMinutes, request.EmployeeId);
        }

        // Step 3: Geo confidence scoring + fraud detection
        GeoConfidenceScorer.GeoScoringResult geoResult = await _geoScorer.ScoreAsync(new GeoConfidenceScorer.GeoScoringRequest(
            request.TenantId,
            request.EmployeeId,
            request.Latitude,
            request.Longitude,
            request.GpsAccuracyMeters,
            request.WifiSsidMatched,
            request.IpWhitelisted,
            request.BleBeaconDetected,
            request.VpnMatched,
            request.MockGpsDetected,
            request.JailbreakDetected,
            request.DeviceFingerprint,
            request.DeviceTimestampUtc), ct);

        // Step 4: Create PunchEvent (immutable)
        var punch = PunchEvent.Record(
            request.TenantId,
            request.EmployeeId,
            request.DeviceId,
            request.DeviceTimestampUtc,
            request.Direction,
            request.CaptureMode,
            request.Modality,
            request.Latitude,
            request.Longitude,
            request.GpsAccuracyMeters,
            geoResult.Fusion.TotalScore,
            request.BiometricMatchScore,
            request.IdempotencyKey,
            clockSkewFlagged,
            geoResult.Fusion.IsSoftFlagged);

        // Hard fraud signals or geo rejection block the punch
        if (geoResult.ShouldBlock)
        {
            string reason = geoResult.FraudSignals.Count > 0
                ? string.Join("; ", geoResult.FraudSignals.Select(f => f.Description))
                : "Geo confidence too low.";

            punch.Reject(reason);
            _db.PunchEvents.Add(punch);
            await _db.SaveChangesAsync(ct);

            _logger.LogWarning(
                "Punch BLOCKED. Employee={EmployeeId} Reason={Reason}", request.EmployeeId, reason);

            // Publish integration event for High/Critical fraud signals — Notifications module subscribes.
            GeoFraudSignal? topSignal = geoResult.FraudSignals
                .OrderByDescending(f => (int)f.Severity)
                .FirstOrDefault();
            if (topSignal is not null && topSignal.Severity >= FraudAlertSeverity.High)
            {
                await _bus.Publish(new GeoFraudDetectedIntegrationEvent(
                    Guid.NewGuid(),
                    request.TenantId,
                    request.EmployeeId,
                    punch.Id,
                    topSignal.Type.ToString(),
                    topSignal.Severity.ToString(),
                    reason,
                    DateTimeOffset.UtcNow), ct);
            }

            return new PunchResult(false, reason, punch.Id, geoResult.Fusion.TotalScore, geoResult.Fusion.Outcome, geoResult.FraudSignals);
        }

        _db.PunchEvents.Add(punch);

        // Step 5: Forward to processing engine
        AttendanceSource source = MapSource(request.CaptureMode);
        GeoPoint? location = request.Latitude.HasValue && request.Longitude.HasValue
            ? new GeoPoint(request.Latitude.Value, request.Longitude.Value)
            : (GeoPoint?)null;

        if (request.Direction == PunchDirection.In)
        {
            await _processingEngine.ProcessCheckInAsync(
                request.TenantId,
                request.EmployeeId,
                punch.AuthoritativeTimestamp,
                location,
                request.DeviceId?.ToString(),
                source,
                ct);
        }
        else
        {
            await _processingEngine.ProcessCheckOutAsync(
                request.TenantId,
                request.EmployeeId,
                punch.AuthoritativeTimestamp,
                location,
                source,
                ct);
        }

        // SaveChanges called inside ProcessCheckIn/Out; call again only if punch wasn't saved
        if (_db.Entry(punch).State == Microsoft.EntityFrameworkCore.EntityState.Added)
            await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Punch accepted. Employee={EmployeeId} Direction={Dir} GeoScore={Score} ClockSkew={Skew}",
            request.EmployeeId, request.Direction, geoResult.Fusion.TotalScore, clockSkewFlagged);

        return new PunchResult(true, null, punch.Id, geoResult.Fusion.TotalScore, geoResult.Fusion.Outcome, geoResult.FraudSignals);
    }

    private static AttendanceSource MapSource(CaptureMode mode) => mode switch
    {
        CaptureMode.Biometric => AttendanceSource.BiometricDevice,
        CaptureMode.Mobile => AttendanceSource.MobileApp,
        CaptureMode.WebPortal => AttendanceSource.WebPortal,
        CaptureMode.SmartCard => AttendanceSource.Kiosk,
        CaptureMode.Manual => AttendanceSource.SystemAutoAction,
        _ => AttendanceSource.WebPortal,
    };
}
