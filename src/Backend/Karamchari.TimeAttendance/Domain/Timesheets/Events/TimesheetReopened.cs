using Karamchari.Core.Domain.Primitives;

namespace Karamchari.TimeAttendance.Domain.Timesheets.Events;

/// <summary>
/// Raised when an already-approved timesheet is reopened for retroactive editing.
/// Downstream systems (Payroll, Revenue) must treat any subsequent
/// <see cref="TimesheetApproved"/> for the same timesheet as a correction.
/// </summary>
public sealed record TimesheetReopened(
    Guid TimesheetId,
    Guid EmployeeId,
    string TenantId,
    DateOnly WeekStartDate,
    Guid AdminId,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
