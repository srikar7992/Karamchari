using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Domain.Workflows;

namespace Karamchari.Workflow.Domain.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record WorkflowInstanceCompleted(
    Guid InstanceId,
    string TenantId,
    string EntityType,
    Guid EntityId,
    WorkflowStatus Status,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
}
