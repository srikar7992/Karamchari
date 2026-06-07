using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Observability;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Resolves which shift assignment a punch belongs to.
/// This is one of the hardest problems in attendance systems:
/// an employee may have 7AM, 9AM, overnight, or swapped shifts on the same day.
///
/// Resolution algorithm (priority order):
/// 1. If punchTime falls within [ExpectedStart - PreWindowMinutes, ExpectedEnd], pick that shift.
/// 2. If multiple shifts overlap the punch window, pick the one with nearest ExpectedStart.
/// 3. If no shift overlaps, pick the shift with nearest ExpectedStart within MaxLookbackMinutes.
/// 4. If still none, punch is unscheduled.
///
/// Overnight shifts: cache entries where ExpectedEnd > ExpectedStart.ToDateOnly() are already
/// normalized by ScheduledShiftCreatedConsumer so ExpectedEnd is on the next calendar day.
/// </summary>
public sealed class ShiftResolver(TimeAttendanceDbContext db, ILogger<ShiftResolver> logger, AttendanceMetrics metrics)
{
    private const int PreWindowMinutes = 60;
    private const int PostWindowMinutes = 60;
    private const int MaxLookbackMinutes = 120;

    private readonly TimeAttendanceDbContext _db = db;
    private readonly ILogger<ShiftResolver> _logger = logger;
    private readonly AttendanceMetrics _metrics = metrics;

    public async Task<ShiftAssignmentCache?> ResolveAsync(
        string tenantId,
        Guid employeeId,
        DateTimeOffset punchTime,
        CancellationToken ct = default)
    {
        var workDate = DateOnly.FromDateTime(punchTime.LocalDateTime);

        // Include yesterday's date to catch overnight shifts (e.g., 22:00 shift still active at 06:15 next morning)
        DateOnly yesterday = workDate.AddDays(-1);

        List<ShiftAssignmentCache> candidates = await _db.ShiftAssignmentCache
            .Where(s =>
                s.TenantId == tenantId &&
                s.EmployeeId == employeeId &&
                (s.WorkDate == workDate || s.WorkDate == yesterday))
            .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            // SLO metric: cache miss — no shift assignments found for this employee+date.
            // Indicates ScheduledShiftCreatedConsumer may be behind, or employee is walk-in.
            _metrics.ShiftResolverCacheMisses.Add(1);
            _logger.LogWarning(
                "ShiftResolver: cache miss. Employee={EmployeeId} PunchTime={PunchTime} WorkDate={WorkDate} Yesterday={Yesterday}",
                employeeId, punchTime, workDate, yesterday);
            return null;
        }

        // Phase 1: shifts whose window [ExpectedStart - pre, ExpectedEnd + post] contains punchTime
        var windowMatches = candidates
            .Where(s =>
                punchTime >= s.ExpectedStart.AddMinutes(-PreWindowMinutes) &&
                punchTime <= s.ExpectedEnd.AddMinutes(PostWindowMinutes))
            .ToList();

        if (windowMatches.Count == 1)
            return windowMatches[0];

        if (windowMatches.Count > 1)
        {
            // Tie-break: nearest ExpectedStart to punchTime
            return windowMatches
                .OrderBy(s => Math.Abs((s.ExpectedStart - punchTime).TotalMinutes))
                .First();
        }

        // Phase 2: no window match — pick nearest ExpectedStart within MaxLookbackMinutes
        ShiftAssignmentCache? nearest = candidates
            .Where(s => Math.Abs((s.ExpectedStart - punchTime).TotalMinutes) <= MaxLookbackMinutes)
            .OrderBy(s => Math.Abs((s.ExpectedStart - punchTime).TotalMinutes))
            .FirstOrDefault();

        if (nearest is null)
            _logger.LogWarning(
                "ShiftResolver: no match within {MaxLookback}min. Employee={EmployeeId} PunchTime={PunchTime} Candidates={CandidateCount}",
                MaxLookbackMinutes, employeeId, punchTime, candidates.Count);
        else
            _logger.LogDebug(
                "ShiftResolver: phase-2 fallback match. Employee={EmployeeId} Shift={ShiftId} PunchTime={PunchTime}",
                employeeId, nearest.ShiftId, punchTime);

        return nearest;
    }
}
