namespace Karamchari.TimeAttendance.Services;

using Karamchari.Core.Projections;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Rebuilds leave-specific projections from authoritative source data.
/// Recovery path when projection tables are corrupted or lost.
///
/// Projections rebuilt:
///   - LeaveCalendarEntry (from approved LeaveRequests)
///   - EmployeeLeaveSummary (from LeaveBalance ledger)
///
/// TeamLeaveProjection requires team headcount data from HR module —
/// that rebuild is triggered via a dedicated endpoint in HR workspace.
/// </summary>
public sealed class LeaveProjectionRebuilder : IProjectionRebuilder
{
    private readonly TimeAttendanceDbContext _db;
    private readonly ILogger<LeaveProjectionRebuilder> _logger;

    public LeaveProjectionRebuilder(TimeAttendanceDbContext db, ILogger<LeaveProjectionRebuilder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public ProjectionMetadata Metadata => new()
    {
        ProjectionName = "LeaveProjections",
        OwningCapability = "TimeAttendance",
        ConsistencyType = ProjectionConsistencyType.Eventual,
        RebuildStrategy = ProjectionRebuildStrategy.FullReplay,
        Category = ProjectionCategory.NearRealTime,
        RefreshTarget = TimeSpan.FromMinutes(15)
    };

    public async Task RebuildAsync(
        string tenantId,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Rebuilding leave projections for tenant {TenantId}", tenantId);

        await RebuildCalendarEntriesAsync(tenantId, ct);
        await RebuildEmployeeSummariesAsync(tenantId, ct);

        _logger.LogInformation("Leave projections rebuild complete for tenant {TenantId}", tenantId);
    }

    private async Task RebuildCalendarEntriesAsync(string tenantId, CancellationToken ct)
    {
        // Delete existing calendar entries for this tenant
        var existing = await _db.LeaveCalendarEntries
            .Where(e => e.TenantId == tenantId)
            .ToListAsync(ct);
        _db.LeaveCalendarEntries.RemoveRange(existing);

        // Rebuild from approved leave requests
        var approvedRequests = await _db.LeaveRequests
            .Where(r => r.TenantId == tenantId && r.Status == LeaveRequestStatus.Approved)
            .ToListAsync(ct);

        foreach (var request in approvedRequests)
        {
            var current = request.StartDate;
            while (current <= request.EndDate)
            {
                var duration = request.Mode == LeaveMode.HalfDay ? 0.5m : 1.0m;
                if (request.Mode == LeaveMode.Hourly && request.HoursRequested.HasValue)
                    duration = (decimal)(request.HoursRequested.Value / 8.0);

                _db.LeaveCalendarEntries.Add(new LeaveCalendarEntry
                {
                    TenantId = tenantId,
                    EmployeeId = request.EmployeeId,
                    Date = current,
                    LeaveRequestId = request.Id,
                    LeaveTypeId = request.PolicyId,
                    Duration = duration,
                    Status = LeaveCalendarEntryStatus.Approved,
                    LastUpdatedAtUtc = DateTimeOffset.UtcNow
                });

                current = current.AddDays(1);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task RebuildEmployeeSummariesAsync(string tenantId, CancellationToken ct)
    {
        // Summaries require year context — rebuild for current year only
        // Full historical rebuild can be triggered per-employee via separate endpoint
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var leaveYear = await _db.LeaveYears
            .Where(y => y.TenantId == tenantId && y.IsActive && y.StartDate <= today && y.EndDate >= today)
            .FirstOrDefaultAsync(ct);

        if (leaveYear is null) return;

        var approvedRequests = await _db.LeaveRequests
            .Where(r => r.TenantId == tenantId
                && r.Status == LeaveRequestStatus.Approved
                && r.StartDate >= leaveYear.StartDate
                && r.EndDate <= leaveYear.EndDate)
            .ToListAsync(ct);

        var grouped = approvedRequests.GroupBy(r => r.EmployeeId);

        foreach (var group in grouped)
        {
            var summary = await _db.EmployeeLeaveSummaries
                .FirstOrDefaultAsync(s => s.TenantId == tenantId
                    && s.EmployeeId == group.Key
                    && s.LeaveYearId == leaveYear.Id, ct);

            if (summary is null)
            {
                summary = new EmployeeLeaveSummary
                {
                    TenantId = tenantId,
                    EmployeeId = group.Key,
                    LeaveYearName = leaveYear.Name,
                    LeaveYearId = leaveYear.Id
                };
                _db.EmployeeLeaveSummaries.Add(summary);
            }

            summary.TotalApprovedRequests = group.Count();
            summary.TotalDaysOnLeaveYtd = (decimal)group.Sum(r => r.ActualDays);
            summary.LastUpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
