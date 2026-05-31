using Karamchari.Core.Domain.Primitives;

namespace Karamchari.TimeAttendance.Domain.Leaves.Events;

public sealed record LeaveRequestCreated(
    Guid RequestId,
    string TenantId,
    Guid EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    double TotalDays) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
