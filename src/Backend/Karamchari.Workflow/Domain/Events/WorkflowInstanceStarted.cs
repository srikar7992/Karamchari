using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Workflow.Domain.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record WorkflowInstanceStarted(
    Guid InstanceId,
    string TenantId,
    string EntityType,
    Guid EntityId,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
}
