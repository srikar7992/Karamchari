using Karamchari.Core.Domain.Primitives;

namespace Karamchari.TimeAttendance.Domain.Attendance.Events;

public sealed record AttendanceRecordCreated(
    Guid AttendanceRecordId,
    string TenantId,
    Guid EmployeeId,
    Guid ShiftId,
    DateOnly WorkDate,
    DateTimeOffset ExpectedStart,
    DateTimeOffset ExpectedEnd) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record AttendanceStatusDetermined(
    Guid AttendanceRecordId,
    string TenantId,
    Guid EmployeeId,
    AttendanceStatus Status,
    int TotalWorkedMinutes) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record AttendanceExceptionRaised(
    Guid ExceptionId,
    Guid AttendanceRecordId,
    string TenantId,
    Guid EmployeeId,
    AttendanceExceptionType ExceptionType,
    AttendanceExceptionSeverity Severity) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record RegularizationSubmitted(
    Guid RequestId,
    string TenantId,
    Guid EmployeeId,
    Guid AttendanceRecordId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record RegularizationApproved(
    Guid RequestId,
    string TenantId,
    Guid EmployeeId,
    Guid AttendanceRecordId,
    Guid ApprovedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record RegularizationRejected(
    Guid RequestId,
    string TenantId,
    Guid EmployeeId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record ReliabilityScoreUpdated(
    Guid EmployeeId,
    string TenantId,
    decimal ReliabilityScore,
    decimal ConsistencyScore,
    decimal ComplianceScore) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

// Integration events published to payroll
public sealed record WorkedMinutesCalculated(
    Guid AttendanceRecordId,
    string TenantId,
    Guid EmployeeId,
    DateOnly WorkDate,
    int TotalWorkedMinutes,
    int OvertimeMinutes) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record AttendanceFinalized(
    Guid AttendanceRecordId,
    string TenantId,
    Guid EmployeeId,
    DateOnly WorkDate,
    AttendanceStatus FinalStatus,
    int TotalWorkedMinutes,
    bool IsRegularized) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record OvertimeDetected(
    Guid AttendanceRecordId,
    string TenantId,
    Guid EmployeeId,
    DateOnly WorkDate,
    int OvertimeMinutes) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
