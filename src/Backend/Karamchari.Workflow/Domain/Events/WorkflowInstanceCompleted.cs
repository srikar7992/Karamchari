using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Workflow.Domain.Events;

public sealed record WorkflowInstanceCompleted(
    Guid InstanceId,
    string TenantId,
    string EntityType,
    Guid EntityId,
    WorkflowStatus Status,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
