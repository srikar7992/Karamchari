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

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid? ShiftId { get; private set; } // Reference to ShiftDefinition
    public DateOnly WorkDate { get; private set; }
    
    public AttendanceStatus Status { get; private set; }
    public DateTimeOffset? CheckInTime { get; private set; }
    public DateTimeOffset? CheckOutTime { get; private set; }

    // Calculated metrics
    public decimal TotalWorkHours { get; private set; }
    public decimal TotalBreakHours { get; private set; }

    public GeoPoint? CheckInLocation { get; private set; }
    public string? CheckInDeviceId { get; private set; }

    public IReadOnlyCollection<AttendanceEvent> Events => _events.AsReadOnly();

    private AttendanceSession() { }

    public static AttendanceSession CreateScheduled(string tenantId, Guid employeeId, Guid shiftId, DateOnly workDate)
    {
        return new AttendanceSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            ShiftId = shiftId,
            WorkDate = workDate,
            Status = AttendanceStatus.Scheduled
        };
    }

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

    public void StartBreak(DateTimeOffset time, AttendanceSource source = AttendanceSource.WebPortal)
    {
        if (Status != AttendanceStatus.CheckedIn)
            throw new InvalidOperationException("Must be checked-in to start break");

        Status = AttendanceStatus.OnBreak;
        _events.Add(AttendanceEvent.Create(Id, time, "Break-Start", source));
    }

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

        var rawDuration = CheckOutTime.Value - CheckInTime.Value;
        
        // Logic to subtract breaks from raw duration
        // Simplified for now
        TotalWorkHours = (decimal)rawDuration.TotalHours;
    }
}

public sealed class AttendanceEvent : Entity<Guid>
{
    public Guid SessionId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public string EventType { get; private set; } = string.Empty;
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
