// -----------------------------------------------------------------------
// <copyright file="ShiftSwap.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.Rostering.Constraints;
using Karamchari.TimeAttendance.Domain.Rostering.Events;

namespace Karamchari.TimeAttendance.Domain.Rostering;

public enum SwapStatus
{
    Requested = 1,
    PendingCounterpartyAccept = 2,
    PendingManagerApproval = 3,
    Approved = 4,
    Rejected = 5,
    Cancelled = 6,
}

public sealed class ShiftSwap : AggregateRoot<Guid>, ITenantOwned
{
    private ShiftSwap() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public Guid RequestedByEmployeeId { get; private set; }
    public Guid RequestedWithEmployeeId { get; private set; }
    public Guid SourceShiftAssignmentId { get; private set; }
    public Guid TargetShiftAssignmentId { get; private set; }
    public SwapStatus Status { get; private set; }
    public Guid? ApprovedByEmployeeId { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset RequestedAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    public static ShiftSwap Request(
        string tenantId,
        Guid requestedBy,
        Guid requestedWith,
        Guid sourceShiftAssignmentId,
        Guid targetShiftAssignmentId)
    {
        var swap = new ShiftSwap
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RequestedByEmployeeId = requestedBy,
            RequestedWithEmployeeId = requestedWith,
            SourceShiftAssignmentId = sourceShiftAssignmentId,
            TargetShiftAssignmentId = targetShiftAssignmentId,
            Status = SwapStatus.PendingCounterpartyAccept,
            RequestedAtUtc = DateTimeOffset.UtcNow,
        };

        swap.RaiseDomainEvent(new ShiftSwapRequested(swap.Id, tenantId, requestedBy, requestedWith));
        return swap;
    }

    public void Accept()
    {
        if (Status != SwapStatus.PendingCounterpartyAccept)
            throw new InvalidOperationException("Swap must be pending counterparty acceptance.");

        Status = SwapStatus.PendingManagerApproval;
    }

    /// <summary>
    /// Approves a swap after revalidating both parties against current constraints.
    /// Conditions may have changed since the swap was requested (e.g. employee took leave).
    /// </summary>
    public void Approve(
        Guid managerId,
        RosterShift sourceShift,
        AssignmentContext requesterContext,
        RosterShift targetShift,
        AssignmentContext counterpartyContext)
    {
        if (Status != SwapStatus.PendingManagerApproval)
            throw new InvalidOperationException("Swap must be pending manager approval.");

        // Revalidate: requester must be eligible for the target shift
        ConstraintResult requesterCheck = AssignmentConstraintEngine.Evaluate(targetShift, requesterContext);
        if (!requesterCheck.IsAllowed)
            throw new InvalidOperationException(
                $"Swap rejected — requester blocked from target shift: [{requesterCheck.ViolationCode}] {requesterCheck.Reason}");

        // Revalidate: counterparty must be eligible for the source shift
        ConstraintResult counterpartyCheck = AssignmentConstraintEngine.Evaluate(sourceShift, counterpartyContext);
        if (!counterpartyCheck.IsAllowed)
            throw new InvalidOperationException(
                $"Swap rejected — counterparty blocked from source shift: [{counterpartyCheck.ViolationCode}] {counterpartyCheck.Reason}");

        Status = SwapStatus.Approved;
        ApprovedByEmployeeId = managerId;
        ResolvedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ShiftSwapApproved(Id, TenantId, RequestedByEmployeeId, RequestedWithEmployeeId, managerId));
    }

    public void Reject(Guid rejectedBy, string reason)
    {
        if (Status is not (SwapStatus.PendingCounterpartyAccept or SwapStatus.PendingManagerApproval))
            throw new InvalidOperationException("Swap cannot be rejected in current state.");

        Status = SwapStatus.Rejected;
        RejectionReason = reason;
        ResolvedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is SwapStatus.Approved or SwapStatus.Rejected or SwapStatus.Cancelled)
            throw new InvalidOperationException("Swap cannot be cancelled in current state.");

        Status = SwapStatus.Cancelled;
        ResolvedAtUtc = DateTimeOffset.UtcNow;
    }
}
