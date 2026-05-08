using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Goals.Events;

public sealed record GoalCycleLocked(
    Guid CycleId,
    string TenantId,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
