using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Workflow.Domain.Events;

public sealed record WorkflowInstanceStarted(
    Guid InstanceId,
    string TenantId,
    string EntityType,
    Guid EntityId,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
