using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Attendance;

/// <summary>
/// Aggregate root for an individual attendance session (workday).
/// Tracks the transition from check-in to check-out.
/// Handles multiple break sessions in one aggregate.
/// </summary>
public sealed class AttendanceSession : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<AttendanceEvent> _events = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    public Guid? ShiftId { get; private set; } // Reference to ShiftDefinition
    // Split-shift support: unique per assignment, not per calendar day.
    // Guid.Empty for walk-in (unscheduled) sessions.
    public Guid ShiftAssignmentId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly WorkDate { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public AttendanceStatus Status { get; private set; }
    public DateTimeOffset? CheckInTime { get; private set; }
    public DateTimeOffset? CheckOutTime { get; private set; }

    // Calculated metrics
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TotalWorkHours { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TotalBreakHours { get; private set; }

    public GeoPoint? CheckInLocation { get; private set; }
    public string? CheckInDeviceId { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<AttendanceEvent> Events => _events.AsReadOnly();

    private AttendanceSession() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static AttendanceSession CreateScheduled(
        string tenantId, Guid employeeId, Guid shiftId, Guid shiftAssignmentId, DateOnly workDate)
    {
        return new AttendanceSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            ShiftId = shiftId,
            ShiftAssignmentId = shiftAssignmentId,
            WorkDate = workDate,
            Status = AttendanceStatus.Scheduled
        };
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void CheckIn(DateTimeOffset time, GeoPoint? location = null, string? deviceId = null, AttendanceSource source = AttendanceSource.WebPortal)
    {
        if (Status != AttendanceStatus.Scheduled && Status != AttendanceStatus.Missed)
            throw new InvalidOperationException($"Cannot check-in from state {Status}");

        CheckInTime = time;
        CheckInLocation = location;
        CheckInDeviceId = deviceId;
        Status = AttendanceStatus.CheckedIn;

        _events.Add(AttendanceEvent.Create(Id, time, "Check-In", source, location));

        // Raise event: AttendanceCheckedIn
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void CheckOut(DateTimeOffset time, GeoPoint? location = null, AttendanceSource source = AttendanceSource.WebPortal)
    {
        if (Status != AttendanceStatus.CheckedIn && Status != AttendanceStatus.ReturnedFromBreak)
            throw new InvalidOperationException($"Cannot check-out from state {Status}");

        if (time <= CheckInTime)
            throw new InvalidOperationException("Check-out time must be after check-in time");

        CheckOutTime = time;
        Status = AttendanceStatus.CheckedOut;

        _events.Add(AttendanceEvent.Create(Id, time, "Check-Out", source, location));

        RecalculateHours();
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void StartBreak(DateTimeOffset time, AttendanceSource source = AttendanceSource.WebPortal)
    {
        if (Status != AttendanceStatus.CheckedIn)
            throw new InvalidOperationException("Must be checked-in to start break");

        Status = AttendanceStatus.OnBreak;
        _events.Add(AttendanceEvent.Create(Id, time, "Break-Start", source));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void EndBreak(DateTimeOffset time, AttendanceSource source = AttendanceSource.WebPortal)
    {
        if (Status != AttendanceStatus.OnBreak)
            throw new InvalidOperationException("Must be on break to return");

        Status = AttendanceStatus.ReturnedFromBreak;
        _events.Add(AttendanceEvent.Create(Id, time, "Break-End", source));

        RecalculateHours();
    }

    private void RecalculateHours()
    {
        if (!CheckInTime.HasValue || !CheckOutTime.HasValue) return;

        TimeSpan rawDuration = CheckOutTime.Value - CheckInTime.Value;

        // Logic to subtract breaks from raw duration
        // Simplified for now
        TotalWorkHours = (decimal)rawDuration.TotalHours;
    }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class AttendanceEvent : Entity<Guid>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid SessionId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset Timestamp { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string EventType { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public AttendanceSource Source { get; private set; }
    public GeoPoint? Location { get; private set; }

    private AttendanceEvent() { }

    internal static AttendanceEvent Create(Guid sessionId, DateTimeOffset time, string type, AttendanceSource source, GeoPoint? loc = null)
    {
        return new AttendanceEvent
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Timestamp = time,
            EventType = type,
            Source = source,
            Location = loc
        };
    }
}
