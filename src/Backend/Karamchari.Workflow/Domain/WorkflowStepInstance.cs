using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Domain.Workflows;

namespace Karamchari.Workflow.Domain;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class WorkflowStepInstance : Entity<Guid>
{
    private WorkflowStepInstance(
        Guid id,
        Guid workflowInstanceId,
        int stepOrder,
        string approverRole)
        : base(id)
    {
        WorkflowInstanceId = workflowInstanceId;
        StepOrder = stepOrder;
        ApproverRole = approverRole;
        ApproverId = Guid.Empty;
        Status = WorkflowStepStatus.Pending;
    }

    private WorkflowStepInstance() { ApproverRole = string.Empty; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid WorkflowInstanceId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int StepOrder { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ApproverRole { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ApproverId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public WorkflowStepStatus Status { get; private set; }
    public DateTimeOffset? ActedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WorkflowStepInstance Create(Guid instanceId, int stepOrder, string approverRole)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approverRole);
        return new WorkflowStepInstance(Guid.NewGuid(), instanceId, stepOrder, approverRole.Trim());
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkApproved(Guid approverId)
    {
        ApproverId = approverId;
        Status = WorkflowStepStatus.Approved;
        ActedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkRejected(Guid approverId, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ApproverId = approverId;
        Status = WorkflowStepStatus.Rejected;
        ActedAt = DateTimeOffset.UtcNow;
        RejectionReason = reason;
    }
}
