namespace Karamchari.TimeAttendance.Persistence;

using Karamchari.Core.Multitenancy;

/// <summary>
/// CQRS read model for manager/ops dashboards.
/// One row per team per date. Updated by consumers of LeaveApproved/LeaveCancelled.
/// Eliminates dynamic aggregation on every dashboard load.
/// </summary>
public sealed class TeamLeaveProjection : ITenantOwned
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;

    /// <summary>Department or team identifier.</summary>
    public Guid TeamId { get; init; }

    public DateOnly Date { get; init; }

    /// <summary>Total headcount scheduled to work this day.</summary>
    public int TotalScheduled { get; set; }

    /// <summary>Number of employees on approved leave.</summary>
    public int OnLeave { get; set; }

    /// <summary>Number of employees available (TotalScheduled - OnLeave).</summary>
    public int Available => TotalScheduled - OnLeave;

    /// <summary>Capacity as percentage of scheduled headcount.</summary>
    public decimal CapacityPercent =>
        TotalScheduled > 0
            ? Math.Round((decimal)Available / TotalScheduled * 100, 1)
            : 0m;

    public DateTimeOffset LastUpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
