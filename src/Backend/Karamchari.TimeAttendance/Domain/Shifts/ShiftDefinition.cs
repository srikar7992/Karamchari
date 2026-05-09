using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Shifts;

/// <summary>
/// Aggregate root for a reusable shift template.
/// Defines when work happens, allowed grace periods, and break rules.
/// </summary>
public sealed class ShiftDefinition : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty; // e.g. "S1", "NIGHT"

    // Local time format (e.g. 09:00 - 18:00)
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }

    // Logic for cross-midnight
    public bool IsOvernight => EndTime < StartTime;

    // Grace periods in minutes
    public int LateArrivalGraceMinutes { get; private set; }
    public int EarlyDepartureGraceMinutes { get; private set; }

    // Minimum work required to mark "Full Day" vs "Half Day"
    public decimal MinimumHoursForFullDay { get; private set; }
    public decimal MinimumHoursForHalfDay { get; private set; }

    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private ShiftDefinition() { }

    public static ShiftDefinition Create(
        string tenantId,
        string name,
        string code,
        TimeOnly start,
        TimeOnly end,
        int lateGrace = 15,
        int earlyGrace = 15)
    {
        return new ShiftDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Code = code,
            StartTime = start,
            EndTime = end,
            LateArrivalGraceMinutes = lateGrace,
            EarlyDepartureGraceMinutes = earlyGrace,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void Deactivate() => IsActive = false;
}
