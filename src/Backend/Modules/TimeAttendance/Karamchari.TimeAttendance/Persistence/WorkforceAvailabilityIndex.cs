using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Persistence;

public enum WorkforceGroupType
{
    Team = 1,
    Department = 2,
    Plant = 3,
    Location = 4,
}

/// <summary>
/// Flagship operational metric — daily availability % per workforce group.
/// Availability = (Scheduled - OnLeave - Absent) / Scheduled * 100.
/// Consumed by manager dashboards, SLA planning, rostering engine.
/// Populated by a consumer that joins AttendanceRecord + LeaveCalendarEntry + WorkforceSchedule.
/// </summary>
public sealed class WorkforceAvailabilityIndex : ITenantOwned
{
    private WorkforceAvailabilityIndex() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public DateOnly Date { get; private set; }
    public WorkforceGroupType GroupType { get; private set; }
    public Guid GroupId { get; private set; }
    public string GroupName { get; private set; } = string.Empty;

    public int Scheduled { get; private set; }
    public int OnLeave { get; private set; }
    public int Absent { get; private set; }
    public int Present { get; private set; }

    /// <summary>
    /// Computed: Present / Scheduled * 100. EF-ignored, computed in app layer.
    /// Formula: Scheduled = 120, Present = 95, Leave = 10, Absent = 15 → 79.1%.
    /// </summary>
    public decimal AvailabilityPercent =>
        Scheduled == 0 ? 0m : Math.Round((decimal)Present / Scheduled * 100m, 1);

    public bool BelowThreshold { get; private set; }
    public decimal ThresholdPercent { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }

    public static WorkforceAvailabilityIndex Compute(
        string tenantId,
        DateOnly date,
        WorkforceGroupType groupType,
        Guid groupId,
        string groupName,
        int scheduled,
        int onLeave,
        int absent,
        decimal thresholdPercent = 70m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        ArgumentOutOfRangeException.ThrowIfNegative(scheduled);

        int present = scheduled - onLeave - absent;
        if (present < 0) present = 0;

        decimal availabilityPercent = scheduled == 0
            ? 100m
            : Math.Round((decimal)present / scheduled * 100m, 1);

        return new WorkforceAvailabilityIndex
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Date = date,
            GroupType = groupType,
            GroupId = groupId,
            GroupName = groupName,
            Scheduled = scheduled,
            OnLeave = onLeave,
            Absent = absent,
            Present = present,
            BelowThreshold = availabilityPercent < thresholdPercent,
            ThresholdPercent = thresholdPercent,
            ComputedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Recomputes when inputs change (e.g., a leave is cancelled same day).</summary>
    public void Refresh(int scheduled, int onLeave, int absent)
    {
        Scheduled = scheduled;
        OnLeave = onLeave;
        Absent = absent;
        Present = Math.Max(0, scheduled - onLeave - absent);
        BelowThreshold = AvailabilityPercent < ThresholdPercent;
        ComputedAt = DateTimeOffset.UtcNow;
    }
}
