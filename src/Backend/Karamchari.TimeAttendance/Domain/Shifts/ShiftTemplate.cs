namespace Karamchari.TimeAttendance.Domain.Shifts;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Defines the structure and rules of a work shift.
/// </summary>
public sealed class ShiftTemplate : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }

    /// <summary>Allowed grace period before marking late (e.g., 15 mins).</summary>
    public int GraceMinutes { get; private set; }

    /// <summary>Minimum minutes required to not be marked absent (e.g. 270 mins / 4.5 hours for half-day).</summary>
    public int HalfDayThresholdMinutes { get; private set; }

    /// <summary>Minimum minutes beyond shift end to qualify as OT (e.g. 120 mins).</summary>
    public int OvertimeThresholdMinutes { get; private set; }

    /// <summary>If true, shift crosses midnight (e.g., 22:00 to 06:00).</summary>
    public bool IsNightShift { get; private set; }

    private ShiftTemplate() { }

    public static ShiftTemplate Create(
        string tenantId,
        string name,
        TimeSpan startTime,
        TimeSpan endTime,
        int graceMinutes,
        int halfDayThreshold,
        int otThreshold,
        bool isNightShift)
    {
        return new ShiftTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            StartTime = startTime,
            EndTime = endTime,
            GraceMinutes = graceMinutes,
            HalfDayThresholdMinutes = halfDayThreshold,
            OvertimeThresholdMinutes = otThreshold,
            IsNightShift = isNightShift
        };
    }
}
