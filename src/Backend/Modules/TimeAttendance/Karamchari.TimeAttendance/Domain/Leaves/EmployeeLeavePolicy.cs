// -----------------------------------------------------------------------
// <copyright file="EmployeeLeavePolicy.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Stores the resolved (hierarchy-flattened) policy assignment for an employee.
/// Recomputed on transfer, promotion, acquisition, or policy change.
/// </summary>
public sealed class EmployeeLeavePolicy : Entity<Guid>, ITenantOwned
{
    private EmployeeLeavePolicy()
    {
        TenantId = string.Empty;
    }

    private EmployeeLeavePolicy(Guid id, Guid employeeId, Guid leaveTypeId, Guid resolvedPolicyId,
        decimal resolvedEntitlement, DateOnly effectiveFrom) : base(id)
    {
        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
        ResolvedPolicyId = resolvedPolicyId;
        ResolvedEntitlement = resolvedEntitlement;
        EffectiveFrom = effectiveFrom;
        IsActive = true;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid LeaveTypeId { get; private set; }

    /// <summary>The policy that won the hierarchy resolution.</summary>
    public Guid ResolvedPolicyId { get; private set; }

    /// <summary>Final entitlement after hierarchy override (e.g. Employee Override = 35 beats Grade = 30).</summary>
    public decimal ResolvedEntitlement { get; private set; }

    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>Reason for this assignment (Transfer, Promotion, Acquisition, Rehire, etc.).</summary>
    public string? AssignmentReason { get; private set; }

    public static EmployeeLeavePolicy Create(Guid employeeId, Guid leaveTypeId, Guid resolvedPolicyId,
        decimal resolvedEntitlement, DateOnly effectiveFrom, string? reason = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(resolvedEntitlement);

        return new EmployeeLeavePolicy(Guid.NewGuid(), employeeId, leaveTypeId, resolvedPolicyId,
            resolvedEntitlement, effectiveFrom)
        {
            AssignmentReason = reason
        };
    }

    public void Supersede(DateOnly supersededOn)
    {
        EffectiveTo = supersededOn;
        IsActive = false;
    }
}
