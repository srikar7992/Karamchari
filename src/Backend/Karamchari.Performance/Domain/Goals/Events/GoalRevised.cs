using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Goals.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record GoalRevised(
    Guid GoalId,
    string TenantId,
    string NewTitle,
    decimal NewTarget,
    string Reason,
    Guid RevisedBy,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
}
