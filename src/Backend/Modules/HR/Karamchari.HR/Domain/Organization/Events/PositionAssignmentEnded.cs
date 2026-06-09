using System;
using Karamchari.Core.Domain.Primitives;

namespace Karamchari.HR.Domain.Organization.Events;

public sealed record PositionAssignmentEnded(
    Guid AssignmentId,
    string TenantId,
    Guid PositionId,
    Guid EmployeeId,
    DateTimeOffset EndUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
