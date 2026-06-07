namespace Karamchari.TimeAttendance.Services;

using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Computes capacity impact before an approval decision is made.
/// Gives manager actionable data: "Approving this will drop team capacity to 58% on May 14."
///
/// Called by approval workflow before presenting decision to manager.
/// </summary>
public sealed class LeaveImpactAssessmentService(TimeAttendanceDbContext db)
{
    private readonly TimeAttendanceDbContext _db = db;

    public async Task<LeaveImpactAssessment> AssessAsync(
        Guid teamId,
        DateOnly leaveStart,
        DateOnly leaveEnd,
        Guid requestingEmployeeId,
        CancellationToken ct = default)
    {
        var criticalDates = new List<DateOnly>();
        decimal minCapacityAfter = 100m;

        DateOnly current = leaveStart;
        while (current <= leaveEnd)
        {
            TeamLeaveProjection? teamProjection = await _db.TeamLeaveProjections
                .FirstOrDefaultAsync(p => p.TeamId == teamId && p.Date == current, ct);

            if (teamProjection is not null && teamProjection.TotalScheduled > 0)
            {
                int onLeaveAfter = teamProjection.OnLeave + 1; // +1 for this request
                decimal capacityAfter = Math.Round((decimal)(teamProjection.TotalScheduled - onLeaveAfter)
                    / teamProjection.TotalScheduled * 100, 1);

                if (capacityAfter < minCapacityAfter) minCapacityAfter = capacityAfter;
                if (capacityAfter < teamProjection.CapacityPercent - 15) criticalDates.Add(current);
            }

            current = current.AddDays(1);
        }

        decimal currentCapacity = await GetCurrentCapacityAsync(teamId, leaveStart, leaveEnd, ct);
        bool belowThreshold = minCapacityAfter < 70m; // Default 70% threshold

        return new LeaveImpactAssessment
        {
            TeamId = teamId,
            StartDate = leaveStart,
            EndDate = leaveEnd,
            CurrentCapacityPercent = currentCapacity,
            CapacityAfterApprovalPercent = minCapacityAfter,
            BelowThreshold = belowThreshold,
            ThresholdPercent = 70,
            CriticalDates = criticalDates
        };
    }

    private async Task<decimal> GetCurrentCapacityAsync(Guid teamId, DateOnly start, DateOnly end, CancellationToken ct)
    {
        List<TeamLeaveProjection> projections = await _db.TeamLeaveProjections
            .Where(p => p.TeamId == teamId && p.Date >= start && p.Date <= end)
            .ToListAsync(ct);

        if (projections.Count == 0) return 100m;
        return projections.Average(p => p.CapacityPercent);
    }
}

public sealed class LeaveImpactAssessment
{
    public Guid TeamId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal CurrentCapacityPercent { get; init; }
    public decimal CapacityAfterApprovalPercent { get; init; }
    public bool BelowThreshold { get; init; }
    public int ThresholdPercent { get; init; }
    public List<DateOnly> CriticalDates { get; init; } = [];
}
