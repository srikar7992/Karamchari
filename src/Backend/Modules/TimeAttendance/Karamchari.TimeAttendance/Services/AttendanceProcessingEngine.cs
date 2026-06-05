using System.Diagnostics;
using System.Text.Json;
using Karamchari.Core.Domain.Primitives;
using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Domain.Schedules;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Core attendance intelligence pipeline.
///
/// Pipeline (each stage independently testable):
///   Punch Received
///     → ShiftResolver (which shift owns this punch?)
///     → AttendanceRecord (lazy create or find existing Pending)
///     → ExceptionEvaluator (policy rule evaluation)
///     → AttendanceScoreCalculator (incremental or full recalc)
///     → AttendanceAuditLog (immutable state change record)
///
/// All DB writes happen in one SaveChangesAsync call per operation.
/// </summary>
public sealed class AttendanceProcessingEngine
{
    private readonly TimeAttendanceDbContext _db;
    private readonly ShiftResolver _shiftResolver;
    private readonly AttendanceScoreCalculator _scoreCalculator;
    private readonly ILogger<AttendanceProcessingEngine> _logger;

    public AttendanceProcessingEngine(
        TimeAttendanceDbContext db,
        ShiftResolver shiftResolver,
        AttendanceScoreCalculator scoreCalculator,
        ILogger<AttendanceProcessingEngine> logger)
    {
        _db = db;
        _shiftResolver = shiftResolver;
        _scoreCalculator = scoreCalculator;
        _logger = logger;
    }

    /// <summary>
    /// Processes a check-in punch.
    /// Resolves shift (ShiftResolver handles multi-shift, overnight, swapped shift cases),
    /// creates AttendanceRecord lazily if not yet present, links session.
    /// </summary>
    public async Task<AttendanceSession> ProcessCheckInAsync(
        string tenantId,
        Guid employeeId,
        DateTimeOffset punchTime,
        GeoPoint? location = null,
        string? deviceId = null,
        AttendanceSource source = AttendanceSource.WebPortal,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // Step 1: Resolve shift
        var shiftAssignment = await _shiftResolver.ResolveAsync(tenantId, employeeId, punchTime, ct);

        // Step 2: Find or create session
        var workDate = DateOnly.FromDateTime(punchTime.LocalDateTime);

        // For overnight shifts the session workDate is the start date, not today's date.
        var sessionWorkDate = shiftAssignment?.WorkDate ?? workDate;

        // Split-shift fix: look up session by ShiftAssignmentId when available.
        // The old (TenantId, EmployeeId, WorkDate) unique index would prevent two sessions on the same day.
        // Now we allow N sessions per day (one per assignment), identified by ShiftAssignmentId.
        AttendanceSession? session;
        if (shiftAssignment != null)
        {
            session = await _db.AttendanceSessions
                .Include(s => s.Events)
                .FirstOrDefaultAsync(s =>
                    s.EmployeeId == employeeId &&
                    s.ShiftAssignmentId == shiftAssignment.ShiftAssignmentId &&
                    (s.Status == AttendanceStatus.Scheduled || s.Status == AttendanceStatus.Missed),
                    ct);
        }
        else
        {
            // Walk-in: no assignment, fall back to date + empty assignment ID
            session = await _db.AttendanceSessions
                .Include(s => s.Events)
                .FirstOrDefaultAsync(s =>
                    s.EmployeeId == employeeId &&
                    s.WorkDate == sessionWorkDate &&
                    s.ShiftAssignmentId == Guid.Empty &&
                    (s.Status == AttendanceStatus.Scheduled || s.Status == AttendanceStatus.Missed),
                    ct);
        }

        var sessionWasNew = session is null;
        if (session is null)
        {
            session = AttendanceSession.CreateScheduled(
                tenantId, employeeId,
                shiftAssignment?.ShiftId ?? Guid.Empty,
                shiftAssignment?.ShiftAssignmentId ?? Guid.Empty,
                sessionWorkDate);
            _db.AttendanceSessions.Add(session);
        }

        session.CheckIn(punchTime, location, deviceId, source);

        // Step 3: Find or lazily create AttendanceRecord
        AttendanceRecord? record = null;

        if (shiftAssignment != null)
        {
            // Check if record already exists for this shift (idempotent re-checkin after Absent mark)
            record = await _db.AttendanceRecords
                .FirstOrDefaultAsync(r =>
                    r.EmployeeId == employeeId &&
                    r.ShiftId == shiftAssignment.ShiftId,
                    ct);

            if (record is null)
            {
                // Lazy creation — first punch for this shift creates the record
                record = AttendanceRecord.CreatePending(
                    tenantId,
                    employeeId,
                    shiftAssignment.ShiftId,
                    shiftAssignment.ShiftAssignmentId,
                    shiftAssignment.WorkDate,
                    shiftAssignment.ExpectedStart,
                    shiftAssignment.ExpectedEnd);
                _db.AttendanceRecords.Add(record);
            }

            record.RecordCheckIn(session.Id, punchTime);

            var audit = AttendanceAuditLog.Record(
                tenantId, record.Id, employeeId,
                "CheckIn",
                AttendanceStatus.Pending.ToString(),
                AttendanceStatus.CheckedIn.ToString(),
                "System",
                $"Check-in via {source} at {punchTime:HH:mm} UTC");
            _db.AttendanceAuditLog.Add(audit);
        }
        else
        {
            _logger.LogWarning(
                "Unscheduled check-in for employee {EmployeeId} at {PunchTime}. No roster assignment resolved.",
                employeeId, punchTime);
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CheckIn processed. Employee={EmployeeId} Shift={ShiftId} SessionNew={SessionNew} ShiftResolved={ShiftResolved} ElapsedMs={ElapsedMs}",
            employeeId, shiftAssignment?.ShiftAssignmentId, sessionWasNew, shiftAssignment != null, sw.ElapsedMilliseconds);

        return session;
    }

    /// <summary>
    /// Processes a check-out punch.
    /// Finds session by current state (not by workDate) so overnight checkouts at 06:15
    /// correctly find the previous day's session without needing workDate matching.
    /// </summary>
    public async Task<AttendanceRecord?> ProcessCheckOutAsync(
        string tenantId,
        Guid employeeId,
        DateTimeOffset punchTime,
        GeoPoint? location = null,
        AttendanceSource source = AttendanceSource.WebPortal,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // Search by session status, not workDate — handles overnight checkout (06:15 next day)
        var session = await _db.AttendanceSessions
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s =>
                s.EmployeeId == employeeId &&
                (s.Status == AttendanceStatus.CheckedIn || s.Status == AttendanceStatus.ReturnedFromBreak),
                ct);

        if (session is null)
        {
            _logger.LogWarning(
                "Check-out for employee {EmployeeId} at {PunchTime} has no active session.",
                employeeId, punchTime);
            return null;
        }

        session.CheckOut(punchTime, location, source);

        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.SessionId == session.Id, ct);

        if (record is null)
        {
            _logger.LogWarning(
                "No AttendanceRecord linked to session {SessionId} for employee {EmployeeId}.",
                session.Id, employeeId);
            await _db.SaveChangesAsync(ct);
            return null;
        }

        var policy = await ResolvePolicy(tenantId, ct);
        var totalWorkedMinutes = ComputeWorkedMinutes(session);

        var shiftDurationMinutes = (int)(record.ExpectedEnd - record.ExpectedStart).TotalMinutes;
        var overtimeMinutes = Math.Max(0, totalWorkedMinutes - shiftDurationMinutes);

        var isLate = record.ActualStart.HasValue &&
                     (record.ActualStart.Value - record.ExpectedStart).TotalMinutes > policy.LateToleranceMinutes;

        AttendanceStatus finalStatus;
        if (totalWorkedMinutes >= shiftDurationMinutes * 0.9m)
            finalStatus = isLate ? AttendanceStatus.Late : AttendanceStatus.Present;
        else if (totalWorkedMinutes >= shiftDurationMinutes * 0.5m)
            finalStatus = AttendanceStatus.HalfDay;
        else
            finalStatus = AttendanceStatus.Absent;

        var previousStatus = record.Status;
        record.RecordCheckOut(punchTime, totalWorkedMinutes, overtimeMinutes, finalStatus);

        // Exception evaluation
        var exceptionDefs = ExceptionEvaluator.Evaluate(record, policy, session.CheckInLocation);
        var exceptions = new List<AttendanceViolation>();
        foreach (var (type, severity, description) in exceptionDefs)
        {
            var ex = AttendanceViolation.Raise(tenantId, record.Id, employeeId, type, severity, description);
            _db.AttendanceViolations.Add(ex);
            exceptions.Add(ex);
        }

        // Audit
        var audit = AttendanceAuditLog.Record(
            tenantId, record.Id, employeeId,
            "CheckOut",
            previousStatus.ToString(),
            finalStatus.ToString(),
            "System",
            $"Check-out via {source}. Worked {totalWorkedMinutes}min, OT {overtimeMinutes}min.");
        _db.AttendanceAuditLog.Add(audit);

        await _scoreCalculator.ApplyIncrementalAsync(
            tenantId, employeeId, finalStatus,
            hadLateArrival: exceptions.Any(e => e.ExceptionType == AttendanceExceptionType.LateArrival),
            hadMissingPunch: false,
            hadUnauthorizedOvertime: exceptions.Any(e => e.ExceptionType == AttendanceExceptionType.Overtime),
            hadLocationViolation: exceptions.Any(e => e.ExceptionType == AttendanceExceptionType.LocationViolation),
            ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CheckOut processed. Employee={EmployeeId} FinalStatus={Status} WorkedMin={WorkedMin} OTMin={OTMin} ElapsedMs={ElapsedMs}",
            employeeId, finalStatus, totalWorkedMinutes, overtimeMinutes, sw.ElapsedMilliseconds);

        return record;
    }

    /// <summary>
    /// End-of-day finalization batch.
    /// Sweeps ShiftAssignmentCache for shifts that ended without any attendance record
    /// (true no-shows), and for sessions still open past grace period (missing checkout).
    ///
    /// Call this nightly after midnight + grace period for the previous calendar day.
    /// </summary>
    public async Task FinalizeShiftsForDateAsync(
        string tenantId,
        DateOnly workDate,
        int gracePeriodMinutes = 30,
        CancellationToken ct = default)
    {
        // Idempotency: this method is safe to retry or run on multiple nodes.
        //   No-show path: existingShiftIds check prevents duplicate Absent records.
        //   Incomplete path: Status == CheckedIn filter skips already-finalized records.
        //   Concurrent nodes: AttendanceRecord.RowVersion causes DbUpdateConcurrencyException
        //     on the second writer; unique index (TenantId, EmployeeId, ShiftId) prevents
        //     duplicate Absent inserts from two nodes racing on the no-show path.
        var sw = Stopwatch.StartNew();
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-gracePeriodMinutes);

        // True no-shows: shifts in cache where no AttendanceRecord was ever created
        var cachedShifts = await _db.ShiftAssignmentCache
            .Where(s =>
                s.TenantId == tenantId &&
                s.WorkDate == workDate &&
                s.ExpectedEnd < cutoff)
            .ToListAsync(ct);

        var existingShiftIds = await _db.AttendanceRecords
            .Where(r =>
                r.TenantId == tenantId &&
                r.WorkDate == workDate)
            .Select(r => r.ShiftId)
            .ToListAsync(ct);

        var noShowShifts = cachedShifts
            .Where(s => !existingShiftIds.Contains(s.ShiftId))
            .ToList();

        foreach (var shift in noShowShifts)
        {
            var record = AttendanceRecord.CreatePending(
                tenantId,
                shift.EmployeeId,
                shift.ShiftId,
                shift.ShiftAssignmentId,
                shift.WorkDate,
                shift.ExpectedStart,
                shift.ExpectedEnd);
            _db.AttendanceRecords.Add(record);
            record.MarkAbsent();

            var noShowEx = AttendanceViolation.Raise(
                tenantId,
                record.Id,
                shift.EmployeeId,
                AttendanceExceptionType.NoShow,
                AttendanceExceptionSeverity.Critical,
                $"Employee did not report for shift on {workDate:yyyy-MM-dd}. Expected: {shift.ExpectedStart:HH:mm}-{shift.ExpectedEnd:HH:mm} UTC.");

            _db.AttendanceViolations.Add(noShowEx);

            var audit = AttendanceAuditLog.Record(
                tenantId, record.Id, shift.EmployeeId,
                "NoShow",
                null,
                AttendanceStatus.Absent.ToString(),
                "System",
                "Finalization job: no check-in recorded past grace period.");
            _db.AttendanceAuditLog.Add(audit);

            await _scoreCalculator.ApplyIncrementalAsync(
                tenantId, shift.EmployeeId, AttendanceStatus.Absent,
                false, false, false, false, ct);
        }

        // Incomplete: sessions still CheckedIn past shift end + grace period
        var incompleteRecords = await _db.AttendanceRecords
            .Where(r =>
                r.TenantId == tenantId &&
                r.WorkDate == workDate &&
                r.Status == AttendanceStatus.CheckedIn &&
                r.ExpectedEnd < cutoff)
            .ToListAsync(ct);

        foreach (var record in incompleteRecords)
        {
            var previousStatus = record.Status;
            var estimatedMinutes = record.ActualStart.HasValue
                ? Math.Max(0, (int)(record.ExpectedEnd - record.ActualStart.Value).TotalMinutes)
                : 0;

            record.MarkIncomplete(estimatedMinutes);

            _db.AttendanceViolations.Add(AttendanceViolation.Raise(
                tenantId,
                record.Id,
                record.EmployeeId,
                AttendanceExceptionType.MissingCheckOut,
                AttendanceExceptionSeverity.High,
                $"No check-out recorded for {workDate:yyyy-MM-dd}. Estimated {estimatedMinutes} worked minutes."));

            _db.AttendanceAuditLog.Add(AttendanceAuditLog.Record(
                tenantId, record.Id, record.EmployeeId,
                "MissingCheckout",
                previousStatus.ToString(),
                AttendanceStatus.Incomplete.ToString(),
                "System",
                "Finalization job: no check-out past grace period."));
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Finalized {WorkDate} for tenant {TenantId}: {NoShowCount} no-shows, {IncompleteCount} incomplete. ElapsedMs={ElapsedMs}",
            workDate, tenantId, noShowShifts.Count, incompleteRecords.Count, sw.ElapsedMilliseconds);
    }

    // Keep old name as alias so scheduled job callers using ProcessNoShowsAsync still compile.
    public Task ProcessNoShowsAsync(string tenantId, DateOnly workDate, CancellationToken ct = default)
        => FinalizeShiftsForDateAsync(tenantId, workDate, 0, ct);

    /// <summary>
    /// Applies an approved regularization to the attendance record.
    /// Called after RegularizationApproved domain event.
    /// </summary>
    public async Task ApplyRegularizationAsync(
        string tenantId,
        Guid attendanceRecordId,
        Guid employeeId,
        Guid approvedByUserId,
        CancellationToken ct = default)
    {
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.Id == attendanceRecordId, ct)
            ?? throw new InvalidOperationException($"AttendanceRecord {attendanceRecordId} not found.");

        var previousStatus = record.Status;

        // Capture before-state for audit trail — supports dispute resolution when status change alone
        // is insufficient (e.g., ActualStart 09:45 → 09:02 after regularization).
        var snapshotBefore = new
        {
            Status = record.Status.ToString(),
            record.ActualStart,
            record.ActualEnd,
            record.TotalWorkedMinutes,
            record.IsRegularized
        };

        record.ApplyRegularization();

        var snapshotAfter = new
        {
            Status = record.Status.ToString(),
            record.ActualStart,
            record.ActualEnd,
            record.TotalWorkedMinutes,
            record.IsRegularized
        };

        var changedFields = JsonSerializer.Serialize(new { Before = snapshotBefore, After = snapshotAfter });

        var openExceptions = await _db.AttendanceViolations
            .Where(e =>
                e.AttendanceRecordId == attendanceRecordId &&
                e.Status == AttendanceExceptionStatus.Open)
            .ToListAsync(ct);

        foreach (var ex in openExceptions)
            ex.Resolve("Resolved by regularization approval.");

        _db.AttendanceAuditLog.Add(AttendanceAuditLog.Record(
            tenantId, record.Id, employeeId,
            "Regularization",
            previousStatus.ToString(),
            AttendanceStatus.Present.ToString(),
            approvedByUserId.ToString(),
            $"Regularization approved. {openExceptions.Count} violations resolved.",
            changedFields));

        await _scoreCalculator.RecalculateAsync(tenantId, employeeId, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Regularization applied for record {RecordId}: {PreviousStatus} → Present. {ExceptionCount} exceptions resolved.",
            attendanceRecordId, previousStatus, openExceptions.Count);
    }

    private async Task<AttendanceShiftPolicy> ResolvePolicy(string tenantId, CancellationToken ct)
    {
        var policy = await _db.AttendanceShiftPolicies
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.IsDefault, ct);

        return policy ?? AttendanceShiftPolicy.CreateDefault(tenantId);
    }

    private static int ComputeWorkedMinutes(AttendanceSession session)
    {
        if (!session.CheckInTime.HasValue) return 0;

        var checkOut = session.CheckOutTime ?? DateTimeOffset.UtcNow;
        var rawMinutes = (int)(checkOut - session.CheckInTime.Value).TotalMinutes;

        var events = session.Events.OrderBy(e => e.Timestamp).ToList();
        var breakMinutes = 0;
        DateTimeOffset? breakStart = null;

        foreach (var e in events)
        {
            if (e.EventType == "Break-Start")
            {
                breakStart = e.Timestamp;
            }
            else if (e.EventType == "Break-End" && breakStart.HasValue)
            {
                breakMinutes += (int)(e.Timestamp - breakStart.Value).TotalMinutes;
                breakStart = null;
            }
        }

        return Math.Max(0, rawMinutes - breakMinutes);
    }
}
