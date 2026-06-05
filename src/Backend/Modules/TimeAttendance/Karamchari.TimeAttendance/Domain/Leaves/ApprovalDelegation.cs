namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Approval delegation when manager is on leave or unavailable.
/// Delegatee receives approval authority within the defined scope and date window.
/// </summary>
public sealed class ApprovalDelegation : AggregateRoot<Guid>, ITenantOwned
{
    private ApprovalDelegation(Guid id, Guid delegatorId, Guid delegateeId,
        DateOnly startDate, DateOnly endDate, DelegationScope scope) : base(id)
    {
        DelegatorId = delegatorId;
        DelegateeId = delegateeId;
        StartDate = startDate;
        EndDate = endDate;
        Scope = scope;
        Status = DelegationStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private ApprovalDelegation()
    {
        TenantId = string.Empty;
    }

    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Manager delegating their approval authority.</summary>
    public Guid DelegatorId { get; private set; }

    /// <summary>Person receiving the approval authority.</summary>
    public Guid DelegateeId { get; private set; }

    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public DelegationScope Scope { get; private set; }

    /// <summary>When Scope = SpecificLeaveType, only this type is delegated.</summary>
    public Guid? ScopedLeaveTypeId { get; private set; }

    public DelegationStatus Status { get; private set; }
    public string? RevocationReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public static ApprovalDelegation Create(Guid delegatorId, Guid delegateeId,
        DateOnly startDate, DateOnly endDate, DelegationScope scope,
        Guid? scopedLeaveTypeId = null)
    {
        if (endDate < startDate)
            throw new ArgumentException("Delegation end date must be on or after start date.");
        if (delegatorId == delegateeId)
            throw new ArgumentException("Delegator and delegatee cannot be the same person.");

        return new ApprovalDelegation(Guid.NewGuid(), delegatorId, delegateeId,
            startDate, endDate, scope)
        {
            ScopedLeaveTypeId = scope == DelegationScope.SpecificLeaveType
                ? scopedLeaveTypeId
                : null
        };
    }

    public void Revoke(string reason)
    {
        if (Status == DelegationStatus.Revoked)
            throw new InvalidOperationException("Delegation already revoked.");
        RevocationReason = reason;
        Status = DelegationStatus.Revoked;
        RevokedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Expire() => Status = DelegationStatus.Expired;

    public bool IsActiveOn(DateOnly date)
        => Status == DelegationStatus.Active
            && date >= StartDate
            && date <= EndDate;

    public bool Covers(Guid leaveTypeId)
        => Scope == DelegationScope.All
            || (Scope == DelegationScope.SpecificLeaveType && ScopedLeaveTypeId == leaveTypeId);
}

public enum DelegationScope
{
    All = 1,
    LeaveOnly = 2,
    SpecificLeaveType = 3
}

public enum DelegationStatus
{
    Active = 1,
    Expired = 2,
    Revoked = 3
}
