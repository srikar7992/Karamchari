using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Reviews.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
}
