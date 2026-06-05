using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Rebuilds ShiftAssignmentCache from roster truth.
///
/// Call this when the cache may be stale — consumer downtime, dead-lettered events,
/// manual DB repair, or after a migration that truncates the cache table.
///
/// Safety: clears existing cache entries for the requested date range and rebuilds
/// from RosterShiftAssignment + RosterShift. Does not touch AttendanceRecords.
/// </summary>
public sealed class ShiftAssignmentCacheRebuilder
{
    private readonly TimeAttendanceDbContext _db;
    private readonly ILogger<ShiftAssignmentCacheRebuilder> _logger;

    public ShiftAssignmentCacheRebuilder(
        TimeAttendanceDbContext db,
        ILogger<ShiftAssignmentCacheRebuilder> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Rebuilds the ShiftAssignmentCache for a tenant and date range from the rostering module's
    /// own tables (RosterShiftAssignments + RosterShifts). This is the authoritative source.
    ///
    /// The cache consumer (ScheduledShiftCreatedConsumer) is the real-time path.
    /// This method is the recovery path for when that consumer falls behind or loses messages.
    /// </summary>
    public async Task<int> RebuildAsync(
        string tenantId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        if (to < from)
            throw new ArgumentException("'to' must be >= 'from'.");

        _logger.LogInformation(
            "CacheRebuilder: starting rebuild for tenant {TenantId} from {From} to {To}.",
            tenantId, from, to);

        // Step 1: Delete existing cache entries for the range.
        // Prevents duplicates if rebuilt multiple times for overlapping windows.
        var deleted = await _db.ShiftAssignmentCache
            .Where(c =>
                c.TenantId == tenantId &&
                c.WorkDate >= from &&
                c.WorkDate <= to)
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation(
            "CacheRebuilder: deleted {Count} stale entries for tenant {TenantId} [{From}–{To}].",
            deleted, tenantId, from, to);

        // Step 2: Join RosterShiftAssignments with their RosterShifts.
        // RosterShift.Id = ShiftId in the cache. RosterShiftAssignment.Id = ShiftAssignmentId.
        var assignments = await _db.RosterShiftAssignments
            .Where(a =>
                a.WorkDate >= from &&
                a.WorkDate <= to &&
                a.Status == Domain.Rostering.AssignmentStatus.Assigned)
            .Join(
                _db.RosterShifts.Where(s => s.TenantId == tenantId),
                a => a.RosterShiftId,
                s => s.Id,
                (a, s) => new
                {
                    ShiftAssignmentId = a.Id,
                    ShiftId = s.Id,
                    a.EmployeeId,
                    s.WorkDate,
                    s.StartTime,
                    s.EndTime,
                    s.IsOvernight
                })
            .ToListAsync(ct);

        var entries = new List<ShiftAssignmentCache>(assignments.Count);
        foreach (var a in assignments)
        {
            var expectedStart = new DateTimeOffset(a.WorkDate.ToDateTime(a.StartTime), TimeSpan.Zero);
            var expectedEnd = a.IsOvernight
                ? new DateTimeOffset(a.WorkDate.AddDays(1).ToDateTime(a.EndTime), TimeSpan.Zero)
                : new DateTimeOffset(a.WorkDate.ToDateTime(a.EndTime), TimeSpan.Zero);

            entries.Add(ShiftAssignmentCache.Create(
                tenantId,
                a.EmployeeId,
                a.ShiftId,
                a.ShiftAssignmentId,
                a.WorkDate,
                expectedStart,
                expectedEnd));
        }

        _db.ShiftAssignmentCache.AddRange(entries);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CacheRebuilder: inserted {Count} entries for tenant {TenantId} [{From}–{To}].",
            entries.Count, tenantId, from, to);

        return entries.Count;
    }
}
