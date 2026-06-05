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
public sealed class AttendanceScoreCalculator
{
    private readonly TimeAttendanceDbContext _db;

    public AttendanceScoreCalculator(TimeAttendanceDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Full recalculation from DB records. Idempotent — safe to call multiple times.
    /// </summary>
    public async Task<AttendanceScore> RecalculateAsync(
        string tenantId,
        Guid employeeId,
        CancellationToken ct = default)
    {
        // Count records for this employee (only finalized, exclude OnLeave/Holiday/WeekOff)
        var records = await _db.AttendanceRecords
            .Where(r => r.EmployeeId == employeeId
                     && r.Status != AttendanceStatus.Pending
                     && r.Status != AttendanceStatus.OnLeave
                     && r.Status != AttendanceStatus.Holiday
                     && r.Status != AttendanceStatus.WeekOff
                     && r.Status != AttendanceStatus.Scheduled)
            .ToListAsync(ct);

        var totalAssigned = records.Count;
        var totalPresent = records.Count(r =>
            r.Status is AttendanceStatus.Present or AttendanceStatus.Late or AttendanceStatus.HalfDay);
        var totalNoShows = records.Count(r => r.Status == AttendanceStatus.Absent);

        // Exceptions tell us the violation details
        var recordIds = records.Select(r => r.Id).ToList();

        var exceptions = await _db.AttendanceViolations
            .Where(e => recordIds.Contains(e.AttendanceRecordId))
            .ToListAsync(ct);

        var totalLate = exceptions.Count(e => e.ExceptionType == AttendanceExceptionType.LateArrival);
        var totalMissingPunches = exceptions.Count(e =>
            e.ExceptionType is AttendanceExceptionType.MissingCheckIn or AttendanceExceptionType.MissingCheckOut);
        var totalUnauthorizedOvertime = exceptions.Count(e => e.ExceptionType == AttendanceExceptionType.Overtime);
        var totalLocationViolations = exceptions.Count(e => e.ExceptionType == AttendanceExceptionType.LocationViolation);

        var score = await _db.AttendanceScores
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
        var score = await _db.AttendanceScores
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
