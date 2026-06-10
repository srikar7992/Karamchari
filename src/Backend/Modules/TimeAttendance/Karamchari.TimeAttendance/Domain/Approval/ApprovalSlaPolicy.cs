// -----------------------------------------------------------------------
// <copyright file="ApprovalSlaPolicy.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Approval;

public enum EscalationAction
{
    NotifyManager = 1,
    ReassignToSkipManager = 2,
    AutoApprove = 3,
    AutoReject = 4,
    NotifyHr = 5,
}

/// <summary>
/// Defines SLA windows and escalation chain for an approval workflow step.
/// Linked to ApprovalWorkflowDefinition by EntityType + StepSequence.
/// ScheduledDomainAction polls for breaches and fires ApprovalSlaBreached events.
/// </summary>
public sealed class ApprovalSlaPolicy : AggregateRoot<Guid>, ITenantOwned
{
    private ApprovalSlaPolicy() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public Guid WorkflowDefinitionId { get; private set; }
    public int StepSequence { get; private set; }
    /// <summary>Hours before first SLA breach is raised.</summary>
    public int FirstBreachHours { get; private set; }
    /// <summary>Hours after first breach before escalation action fires.</summary>
    public int EscalationHours { get; private set; }
    public EscalationAction EscalationAction { get; private set; }
    /// <summary>Role to reassign/notify on escalation (e.g. "SkipManager", "HR").</summary>
    public string EscalationTargetRole { get; private set; } = string.Empty;
    public bool AutoActionEnabled { get; private set; }
    public bool IsActive { get; private set; }

    public static ApprovalSlaPolicy Create(
        string tenantId,
        Guid workflowDefinitionId,
        int stepSequence,
        int firstBreachHours,
        int escalationHours,
        EscalationAction escalationAction,
        string escalationTargetRole,
        bool autoActionEnabled = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(escalationTargetRole);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(firstBreachHours);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(escalationHours);

        return new ApprovalSlaPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WorkflowDefinitionId = workflowDefinitionId,
            StepSequence = stepSequence,
            FirstBreachHours = firstBreachHours,
            EscalationHours = escalationHours,
            EscalationAction = escalationAction,
            EscalationTargetRole = escalationTargetRole,
            AutoActionEnabled = autoActionEnabled,
            IsActive = true,
        };
    }

    public void Deactivate() => IsActive = false;

    public DateTimeOffset BreachDeadline(DateTimeOffset stepStartedAt) =>
        stepStartedAt.AddHours(FirstBreachHours);

    public DateTimeOffset EscalationDeadline(DateTimeOffset stepStartedAt) =>
        stepStartedAt.AddHours(FirstBreachHours + EscalationHours);
}

/// <summary>
/// Records each SLA breach event on an approval step.
/// One row per breach occurrence — supports repeat-breach tracking.
/// </summary>
public sealed class ApprovalEscalation : ITenantOwned
{
    private ApprovalEscalation() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public Guid LeaveApprovalId { get; private set; }
    public Guid LeaveRequestId { get; private set; }
    public int StepSequence { get; private set; }
    public Guid OriginalApproverId { get; private set; }
    public string BreachType { get; private set; } = string.Empty;
    public DateTimeOffset BreachedAt { get; private set; }
    public EscalationAction ActionTaken { get; private set; }
    public Guid? EscalatedToApproverId { get; private set; }
    public bool Resolved { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public static ApprovalEscalation Raise(
        string tenantId,
        Guid leaveApprovalId,
        Guid leaveRequestId,
        int stepSequence,
        Guid originalApproverId,
        string breachType,
        EscalationAction actionTaken,
        Guid? escalatedToApproverId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(breachType);

        return new ApprovalEscalation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LeaveApprovalId = leaveApprovalId,
            LeaveRequestId = leaveRequestId,
            StepSequence = stepSequence,
            OriginalApproverId = originalApproverId,
            BreachType = breachType,
            BreachedAt = DateTimeOffset.UtcNow,
            ActionTaken = actionTaken,
            EscalatedToApproverId = escalatedToApproverId,
            Resolved = false,
        };
    }

    public void MarkResolved()
    {
        Resolved = true;
        ResolvedAt = DateTimeOffset.UtcNow;
    }
}
