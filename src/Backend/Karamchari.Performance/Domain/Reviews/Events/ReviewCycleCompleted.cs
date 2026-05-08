using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Reviews.Events;

public sealed record ReviewCycleCompleted(
    Guid CycleId,
    string TenantId,
    string CycleName,
    DateOnly ReviewPeriodStart,
    DateOnly ReviewPeriodEnd,
    int TotalAssignments,
    int CompletedAssignments,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
