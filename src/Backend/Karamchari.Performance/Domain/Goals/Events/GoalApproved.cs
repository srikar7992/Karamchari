using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Goals.Events;

public sealed record GoalApproved(
    Guid GoalId,
    string TenantId,
    Guid OwnerId,
    GoalOwnerType OwnerType,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
