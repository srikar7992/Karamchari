using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Workflow.Domain.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record WorkflowStepAdvanced(
    Guid InstanceId,
    string TenantId,
    int FromStep,
    int ToStep,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
}
