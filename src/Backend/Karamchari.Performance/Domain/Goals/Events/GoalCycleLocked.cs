using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Goals.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record GoalCycleLocked(
    Guid CycleId,
    string TenantId,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
}
