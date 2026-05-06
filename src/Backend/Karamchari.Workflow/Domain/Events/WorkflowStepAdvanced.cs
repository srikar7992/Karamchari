using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Workflow.Domain.Events;

public sealed record WorkflowStepAdvanced(
    Guid InstanceId,
    string TenantId,
    int FromStep,
    int ToStep,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
