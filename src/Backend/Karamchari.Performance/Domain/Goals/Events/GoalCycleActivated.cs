using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Goals.Events;

public sealed record GoalCycleActivated(
    Guid CycleId,
    string TenantId,
    string CycleName,
    DateOnly StartDate,
    DateOnly EndDate,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
