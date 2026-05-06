using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Workflow.Domain;

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
        Status = StepInstanceStatus.Pending;
    }

    private WorkflowStepInstance() { ApproverRole = string.Empty; }

    public Guid WorkflowInstanceId { get; private set; }
    public int StepOrder { get; private set; }
    public string ApproverRole { get; private set; }
    public Guid ApproverId { get; private set; }
    public StepInstanceStatus Status { get; private set; }
    public DateTimeOffset? ActedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    public static WorkflowStepInstance Create(Guid instanceId, int stepOrder, string approverRole)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approverRole);
        return new WorkflowStepInstance(Guid.NewGuid(), instanceId, stepOrder, approverRole.Trim());
    }

    public void MarkApproved(Guid approverId)
    {
        ApproverId = approverId;
        Status = StepInstanceStatus.Approved;
        ActedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRejected(Guid approverId, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ApproverId = approverId;
        Status = StepInstanceStatus.Rejected;
        ActedAt = DateTimeOffset.UtcNow;
        RejectionReason = reason;
    }
}
