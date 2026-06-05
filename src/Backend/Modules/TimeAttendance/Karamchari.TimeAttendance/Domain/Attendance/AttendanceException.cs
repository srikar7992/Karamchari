using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.Attendance.Events;

namespace Karamchari.TimeAttendance.Domain.Attendance;

public enum AttendanceExceptionType
{
    LateArrival,
    EarlyExit,
    MissingCheckIn,
    MissingCheckOut,
    NoShow,
    Overtime,
    ShiftMismatch,
    LocationViolation
}

public enum AttendanceExceptionSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum AttendanceExceptionStatus
{
    Open,
    Acknowledged,
    Resolved,
    Waived
}

/// <summary>
/// Attendance policy violation on a specific attendance record.
/// Managers act on violations, not on the 500 successful attendances.
/// Severity is determined by policy thresholds at creation time and is immutable.
/// Named AttendanceViolation to avoid CA1711 (name cannot end in Exception).
/// Domain concept corresponds to spec's AttendanceException aggregate.
/// </summary>
public sealed class AttendanceViolation : AggregateRoot<Guid>, ITenantOwned
{
    private AttendanceViolation() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid AttendanceRecordId { get; private set; }
    public Guid EmployeeId { get; private set; }

    public AttendanceExceptionType ExceptionType { get; private set; }
    public AttendanceExceptionSeverity Severity { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public AttendanceExceptionStatus Status { get; private set; }

    public string? AcknowledgedBy { get; private set; }
    public string? Resolution { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Creates a violation with severity computed from spec rules + policy thresholds.
    /// Raises AttendanceExceptionRaised domain event.
    /// </summary>
    public static AttendanceViolation Raise(
        string tenantId,
        Guid attendanceRecordId,
        Guid employeeId,
        AttendanceExceptionType type,
        AttendanceExceptionSeverity severity,
        string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Violation description required.", nameof(description));

        var violation = new AttendanceViolation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AttendanceRecordId = attendanceRecordId,
            EmployeeId = employeeId,
            ExceptionType = type,
            Severity = severity,
            Description = description,
            Status = AttendanceExceptionStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        violation.RaiseDomainEvent(new AttendanceExceptionRaised(
            violation.Id, attendanceRecordId, tenantId, employeeId, type, severity));

        return violation;
    }

    public void Acknowledge(string byUserId)
    {
        if (Status != AttendanceExceptionStatus.Open)
            throw new InvalidOperationException($"Violation already {Status}.");

        Status = AttendanceExceptionStatus.Acknowledged;
        AcknowledgedBy = byUserId;
    }

    public void Resolve(string resolution)
    {
        if (Status == AttendanceExceptionStatus.Resolved || Status == AttendanceExceptionStatus.Waived)
            throw new InvalidOperationException($"Violation already {Status}.");

        Status = AttendanceExceptionStatus.Resolved;
        Resolution = resolution;
        ResolvedAt = DateTimeOffset.UtcNow;
    }

    public void Waive(string reason)
    {
        if (Status == AttendanceExceptionStatus.Resolved || Status == AttendanceExceptionStatus.Waived)
            throw new InvalidOperationException($"Violation already {Status}.");

        Status = AttendanceExceptionStatus.Waived;
        Resolution = reason;
        ResolvedAt = DateTimeOffset.UtcNow;
    }

}
