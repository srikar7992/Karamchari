using Karamchari.Core.Domain.Primitives;

namespace Karamchari.HR.Domain.Departments.Events;

public sealed record DepartmentCreated(
    Guid DepartmentId,
    string Name,
    string? Description) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
