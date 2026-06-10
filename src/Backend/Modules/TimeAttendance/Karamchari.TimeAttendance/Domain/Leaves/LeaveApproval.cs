// -----------------------------------------------------------------------
// <copyright file="LeaveApproval.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Approval chain for a single leave request.
/// Supports sequential multi-level approval: Manager → HR → Skip-Level → Compliance.
/// One LeaveApproval aggregate per LeaveRequest.
/// </summary>
public sealed class LeaveApproval : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<LeaveApprovalStep> _steps = [];

    private LeaveApproval(Guid id, Guid leaveRequestId) : base(id)
    {
        LeaveRequestId = leaveRequestId;
        Status = ApprovalChainStatus.InProgress;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private LeaveApproval()
    {
        TenantId = string.Empty;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid LeaveRequestId { get; private set; }
    public ApprovalChainStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public IReadOnlyCollection<LeaveApprovalStep> Steps => _steps.AsReadOnly();

    public LeaveApprovalStep? CurrentStep =>
        _steps.Where(s => s.Decision == ApprovalDecision.Pending)
              .OrderBy(s => s.Sequence)
              .FirstOrDefault();

    public static LeaveApproval Create(Guid leaveRequestId)
        => new(Guid.NewGuid(), leaveRequestId);

    /// <summary>Adds an approver to the chain at a given sequence position.</summary>
    public void AddStep(int sequence, Guid approverId, ApproverRole role, string? comments = null)
    {
        if (Status != ApprovalChainStatus.InProgress)
            throw new InvalidOperationException("Cannot modify a completed approval chain.");
        if (_steps.Any(s => s.Sequence == sequence))
            throw new InvalidOperationException($"Sequence {sequence} already exists.");

        _steps.Add(LeaveApprovalStep.Create(Id, sequence, approverId, role, comments));
    }

    /// <summary>Records a decision by an approver at the given sequence.</summary>
    public void RecordDecision(int sequence, ApprovalDecision decision, string? comments = null)
    {
        if (Status != ApprovalChainStatus.InProgress)
            throw new InvalidOperationException("Chain already completed.");

        LeaveApprovalStep step = _steps.FirstOrDefault(s => s.Sequence == sequence)
            ?? throw new InvalidOperationException($"Step {sequence} not found.");

        step.Decide(decision, comments);

        if (decision == ApprovalDecision.Rejected)
        {
            Status = ApprovalChainStatus.Rejected;
            CompletedAtUtc = DateTimeOffset.UtcNow;
            return;
        }

        // Check if all steps approved
        bool remaining = _steps.Any(s => s.Decision == ApprovalDecision.Pending);
        if (!remaining)
        {
            Status = ApprovalChainStatus.Approved;
            CompletedAtUtc = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Reassigns pending steps from one approver to another (manager resignation scenario).
    /// </summary>
    public void ReassignPendingSteps(Guid fromApproverId, Guid toApproverId, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        foreach (LeaveApprovalStep? step in _steps.Where(s =>
            s.ApproverId == fromApproverId && s.Decision == ApprovalDecision.Pending))
        {
            step.Reassign(toApproverId, reason);
        }
    }
}

public sealed class LeaveApprovalStep : Entity<Guid>
{
    private LeaveApprovalStep() { }

    private LeaveApprovalStep(Guid id, Guid approvalId, int sequence,
        Guid approverId, ApproverRole role) : base(id)
    {
        ApprovalId = approvalId;
        Sequence = sequence;
        ApproverId = approverId;
        Role = role;
        Decision = ApprovalDecision.Pending;
    }

    public Guid ApprovalId { get; private set; }
    public int Sequence { get; private set; }
    public Guid ApproverId { get; private set; }
    public ApproverRole Role { get; private set; }
    public ApprovalDecision Decision { get; private set; }
    public string? Comments { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    public Guid? ReassignedFromApproverId { get; private set; }
    public string? ReassignmentReason { get; private set; }

    internal static LeaveApprovalStep Create(Guid approvalId, int sequence,
        Guid approverId, ApproverRole role, string? comments)
        => new(Guid.NewGuid(), approvalId, sequence, approverId, role)
        {
            Comments = comments
        };

    internal void Decide(ApprovalDecision decision, string? comments)
    {
        if (Decision != ApprovalDecision.Pending)
            throw new InvalidOperationException("Step already decided.");
        Decision = decision;
        Comments = comments;
        DecidedAtUtc = DateTimeOffset.UtcNow;
    }

    internal void Reassign(Guid toApproverId, string reason)
    {
        ReassignedFromApproverId = ApproverId;
        ReassignmentReason = reason;
        ApproverId = toApproverId;
    }
}

public enum ApprovalChainStatus
{
    InProgress = 1,
    Approved = 2,
    Rejected = 3
}

public enum ApprovalDecision
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum ApproverRole
{
    Manager = 1,
    HR = 2,
    SkipLevel = 3,
    Compliance = 4,
    SystemAuto = 5
}
