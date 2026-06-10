// -----------------------------------------------------------------------
// <copyright file="ApprovalEngine.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Core.Approvals;

/// <summary>
/// States for an approval request instance.
/// </summary>
public enum ApprovalStatus
{
    /// <summary>Approval is pending decision</summary>
    Pending,
    /// <summary>Approved by all stages</summary>
    Approved,
    /// <summary>Rejected by any stage</summary>
    Rejected
}

/// <summary>
/// Definition template for an approval flow (e.g. Compensation Revision, Opportunity Admission).
/// </summary>
public sealed class ApprovalDefinition : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ApprovalStage> _stages = [];

    private ApprovalDefinition()
    {
        TenantId = string.Empty;
        Domain = string.Empty;
        ResourceType = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalDefinition"/> class.
    /// </summary>
    public ApprovalDefinition(Guid id, string tenantId, string domain, string resourceType) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);

        TenantId = tenantId;
        Domain = domain;
        ResourceType = resourceType;
    }

    /// <inheritdoc />
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the originating domain name (e.g. Capability, Compensation).
    /// </summary>
    public string Domain { get; private set; }

    /// <summary>
    /// Gets the type of resource being approved (e.g. Opportunity, SalaryRevision).
    /// </summary>
    public string ResourceType { get; private set; }

    /// <summary>
    /// Gets the approval stages required for this definition.
    /// </summary>
    public IReadOnlyCollection<ApprovalStage> Stages => _stages.AsReadOnly();

    /// <summary>
    /// Adds a required stage to the approval workflow definition.
    /// </summary>
    public void AddStage(Guid approverRoleId, int order)
    {
        _stages.Add(new ApprovalStage(Guid.NewGuid(), Id, approverRoleId, order));
    }
}

/// <summary>
/// A level or step in an approval definition hierarchy.
/// </summary>
public sealed class ApprovalStage : Entity<Guid>
{
    private ApprovalStage() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalStage"/> class.
    /// </summary>
    public ApprovalStage(Guid id, Guid approvalDefinitionId, Guid approverRoleId, int order) : base(id)
    {
        ApprovalDefinitionId = approvalDefinitionId;
        ApproverRoleId = approverRoleId;
        Order = order;
    }

    /// <summary>
    /// Gets the approval definition template ID.
    /// </summary>
    public Guid ApprovalDefinitionId { get; }

    /// <summary>
    /// Gets the required approver role ID.
    /// </summary>
    public Guid ApproverRoleId { get; }

    /// <summary>
    /// Gets the sequence ordering position for this stage.
    /// </summary>
    public int Order { get; }
}

/// <summary>
/// A concrete instance of an approval flow for a specific entity ID.
/// </summary>
public sealed class ApprovalInstance : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ApprovalDecision> _decisions = [];

    private ApprovalInstance()
    {
        TenantId = string.Empty;
    }

    /// <summary>
    /// Gets the date and time when this approval request instance is due.
    /// </summary>
    public DateTimeOffset? DueUtc { get; private set; }

    /// <summary>
    /// Updates the due date/time of this approval instance.
    /// </summary>
    public void SetDueUtc(DateTimeOffset? dueUtc)
    {
        DueUtc = dueUtc;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalInstance"/> class.
    /// </summary>
    public ApprovalInstance(Guid id, string tenantId, Guid approvalDefinitionId, Guid resourceId) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        TenantId = tenantId;
        ApprovalDefinitionId = approvalDefinitionId;
        ResourceId = resourceId;
        Status = ApprovalStatus.Pending;
    }

    /// <inheritdoc />
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the definition template ID.
    /// </summary>
    public Guid ApprovalDefinitionId { get; private set; }

    /// <summary>
    /// Gets the reference ID of the domain entity undergoing approval.
    /// </summary>
    public Guid ResourceId { get; private set; }

    /// <summary>
    /// Gets the current status of the approval flow.
    /// </summary>
    public ApprovalStatus Status { get; private set; }

    /// <summary>
    /// Gets the decisions recorded for this approval instance.
    /// </summary>
    public IReadOnlyCollection<ApprovalDecision> Decisions => _decisions.AsReadOnly();

    /// <summary>
    /// Records a decision decision from an employee.
    /// </summary>
    public void RecordDecision(Guid approverEmployeeId, int level, string action, string comments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        _decisions.Add(new ApprovalDecision(Guid.NewGuid(), Id, approverEmployeeId, level, action, comments ?? string.Empty));
    }

    /// <summary>
    /// Marks the instance as approved.
    /// </summary>
    public void Approve() => Status = ApprovalStatus.Approved;

    /// <summary>
    /// Marks the instance as rejected.
    /// </summary>
    public void Reject() => Status = ApprovalStatus.Rejected;
}

/// <summary>
/// Represents an individual decision recorded on an approval instance stage.
/// </summary>
public sealed class ApprovalDecision : Entity<Guid>
{
    private ApprovalDecision()
    {
        Action = string.Empty;
        Comments = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalDecision"/> class.
    /// </summary>
    public ApprovalDecision(Guid id, Guid approvalInstanceId, Guid approverEmployeeId, int level, string action, string comments) : base(id)
    {
        ApprovalInstanceId = approvalInstanceId;
        ApproverEmployeeId = approverEmployeeId;
        Level = level;
        Action = action;
        Comments = comments ?? string.Empty;
        DecidedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the parent approval instance ID.
    /// </summary>
    public Guid ApprovalInstanceId { get; }

    /// <summary>
    /// Gets the employee ID who made the decision.
    /// </summary>
    public Guid ApproverEmployeeId { get; }

    /// <summary>
    /// Gets the workflow level stage index.
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// Gets the action performed (Approve, Reject, Delegate, Escalate).
    /// </summary>
    public string Action { get; private set; }

    /// <summary>
    /// Gets decision comments.
    /// </summary>
    public string Comments { get; private set; }

    /// <summary>
    /// Gets the decision timestamp.
    /// </summary>
    public DateTimeOffset DecidedAtUtc { get; private set; }
}

/// <summary>
/// Authority delegation record for approvals when users are unavailable.
/// </summary>
public sealed class ApprovalDelegation : AggregateRoot<Guid>, ITenantOwned
{
    private ApprovalDelegation()
    {
        TenantId = string.Empty;
        CreatedBy = string.Empty;
        Reason = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalDelegation"/> class.
    /// </summary>
    public ApprovalDelegation(
        Guid id,
        string tenantId,
        Guid delegatorId,
        Guid delegateId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string createdBy,
        string reason) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        TenantId = tenantId;
        DelegatorId = delegatorId;
        DelegateId = delegateId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        CreatedBy = createdBy;
        Reason = reason ?? string.Empty;
        CreatedUtc = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    /// <inheritdoc />
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the original delegator employee ID.
    /// </summary>
    public Guid DelegatorId { get; private set; }

    /// <summary>
    /// Gets the delegate employee ID who receives authority.
    /// </summary>
    public Guid DelegateId { get; private set; }

    /// <summary>
    /// Gets the start date of the delegation boundary.
    /// </summary>
    public DateTimeOffset StartUtc { get; private set; }

    /// <summary>
    /// Gets the end date of the delegation boundary.
    /// </summary>
    public DateTimeOffset EndUtc { get; private set; }

    /// <summary>
    /// Gets a value indicating whether delegation remains active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the operator identifier who created this delegation.
    /// </summary>
    public string CreatedBy { get; private set; }

    /// <summary>
    /// Gets the timestamp when this delegation was created.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; private set; }

    /// <summary>
    /// Gets the business justification or reason for delegation.
    /// </summary>
    public string Reason { get; private set; }

    /// <summary>
    /// Deactivates the delegation mapping.
    /// </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Checks if this delegation overlaps with the specified period.
    /// </summary>
    public bool OverlapsWith(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        return IsActive && StartUtc < endUtc && startUtc < EndUtc;
    }

    /// <summary>
    /// Creates a new approval delegation, enforcing overlap invariants.
    /// </summary>
    public static ApprovalDelegation Create(
        Guid id,
        string tenantId,
        Guid delegatorId,
        Guid delegateId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string createdBy,
        string reason,
        IEnumerable<ApprovalDelegation> existingDelegations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);
        if (delegateId == delegatorId)
        {
            throw new InvalidOperationException("Delegator and delegate cannot be the same person.");
        }
        if (endUtc <= startUtc)
        {
            throw new InvalidOperationException("Delegation end time must be after the start time.");
        }

        foreach (var existing in existingDelegations)
        {
            if (existing.DelegatorId == delegatorId && existing.OverlapsWith(startUtc, endUtc))
            {
                throw new InvalidOperationException($"An active delegation already exists for this delegator within the overlapping period (Existing: {existing.StartUtc:u} to {existing.EndUtc:u}).");
            }
        }

        return new ApprovalDelegation(id, tenantId, delegatorId, delegateId, startUtc, endUtc, createdBy, reason);
    }
}
