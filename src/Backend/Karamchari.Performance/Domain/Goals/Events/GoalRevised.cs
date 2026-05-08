using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Goals.Events;

public sealed record GoalRevised(
    Guid GoalId,
    string TenantId,
    string NewTitle,
    decimal NewTarget,
    string Reason,
    Guid RevisedBy,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
