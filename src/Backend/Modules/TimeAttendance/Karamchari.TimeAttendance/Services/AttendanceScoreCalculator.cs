using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Computes and persists AttendanceScore for an employee.
/// Uses full recount from DB to ensure accuracy.
/// Incremental updates (via AttendanceScore.ApplyAttendanceResult) are used for real-time path;
/// full recalculation is used by the nightly batch and after regularization approvals.
/// </summary>
public sealed class AttendanceScoreCalculator(TimeAttendanceDbContext db)
{
    private readonly TimeAttendanceDbContext _db = db;

    /// <summary>
    /// Full recalculation from DB records. Idempotent — safe to call multiple times.
    /// </summary>
    public async Task<AttendanceScore> RecalculateAsync(
        string tenantId,
        Guid employeeId,
        CancellationToken ct = default)
    {
        // Count records for this employee (only finalized, exclude OnLeave/Holiday/WeekOff)
        List<AttendanceRecord> records = await _db.AttendanceRecords
            .Where(r => r.EmployeeId == employeeId
                     && r.Status != AttendanceStatus.Pending
                     && r.Status != AttendanceStatus.OnLeave
                     && r.Status != AttendanceStatus.Holiday
                     && r.Status != AttendanceStatus.WeekOff
                     && r.Status != AttendanceStatus.Scheduled)
            .ToListAsync(ct);

        int totalAssigned = records.Count;
        int totalPresent = records.Count(r =>
            r.Status is AttendanceStatus.Present or AttendanceStatus.Late or AttendanceStatus.HalfDay);
        int totalNoShows = records.Count(r => r.Status == AttendanceStatus.Absent);

        // Exceptions tell us the violation details
        var recordIds = records.Select(r => r.Id).ToList();

        List<AttendanceViolation> exceptions = await _db.AttendanceViolations
            .Where(e => recordIds.Contains(e.AttendanceRecordId))
            .ToListAsync(ct);

        int totalLate = exceptions.Count(e => e.ExceptionType == AttendanceExceptionType.LateArrival);
        int totalMissingPunches = exceptions.Count(e =>
            e.ExceptionType is AttendanceExceptionType.MissingCheckIn or AttendanceExceptionType.MissingCheckOut);
        int totalUnauthorizedOvertime = exceptions.Count(e => e.ExceptionType == AttendanceExceptionType.Overtime);
        int totalLocationViolations = exceptions.Count(e => e.ExceptionType == AttendanceExceptionType.LocationViolation);

        AttendanceScore? score = await _db.AttendanceScores
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId, ct);

        if (score is null)
        {
            score = AttendanceScore.Create(tenantId, employeeId);
            _db.AttendanceScores.Add(score);
        }

        score.Recalculate(
            totalAssigned,
            totalPresent,
            totalLate,
            totalNoShows,
            totalMissingPunches,
            totalUnauthorizedOvertime,
            totalLocationViolations);

        return score;
    }

    /// <summary>
    /// Delta adjustment for regularization approvals. O(1) — no record scan.
    /// Uses stored counters on AttendanceScore to compute the delta.
    /// Falls back to RecalculateAsync on nightly batch if counters ever drift.
    /// </summary>
    public async Task<AttendanceScore> ApplyRegularizationDeltaAsync(
        string tenantId,
        Guid employeeId,
        AttendanceStatus previousStatus,
        IReadOnlyList<AttendanceExceptionType> resolvedViolations,
        CancellationToken ct = default)
    {
        AttendanceScore? score = await _db.AttendanceScores
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId, ct);

        if (score is null)
        {
            score = AttendanceScore.Create(tenantId, employeeId);
            _db.AttendanceScores.Add(score);
        }

        score.ApplyRegularizationDelta(previousStatus, resolvedViolations);

        return score;
    }

    /// <summary>
    /// Incremental update — faster path for real-time use. Called after each punch finalization.
    /// </summary>
    public async Task<AttendanceScore> ApplyIncrementalAsync(
        string tenantId,
        Guid employeeId,
        AttendanceStatus finalStatus,
        bool hadLateArrival,
        bool hadMissingPunch,
        bool hadUnauthorizedOvertime,
        bool hadLocationViolation,
        CancellationToken ct = default)
    {
        AttendanceScore? score = await _db.AttendanceScores
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId, ct);

        if (score is null)
        {
            score = AttendanceScore.Create(tenantId, employeeId);
            _db.AttendanceScores.Add(score);
        }

        score.ApplyAttendanceResult(finalStatus, hadLateArrival, hadMissingPunch, hadUnauthorizedOvertime, hadLocationViolation);

        return score;
    }
}
