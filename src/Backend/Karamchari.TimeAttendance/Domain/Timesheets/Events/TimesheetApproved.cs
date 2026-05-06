using Karamchari.Core.Domain.Primitives;

namespace Karamchari.TimeAttendance.Domain.Timesheets.Events;

/// <summary>
/// Raised when a timesheet transitions to Approved status.
/// Captured by the MassTransit outbox inside the same SaveChanges transaction,
/// then consumed by <c>TimesheetApprovedConsumer</c> which publishes the
/// public integration event for Payroll, Revenue Ledger, and Utilization contexts.
/// </summary>
public sealed record TimesheetApproved(
    Guid TimesheetId,
    Guid EmployeeId,
    string TenantId,
    DateOnly WeekStartDate,
    string EmployeeTimeZoneId,
    IReadOnlyList<TimeEntry> Entries,
    bool IsRetroactive) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
