// -----------------------------------------------------------------------
// <copyright file="LeavePolicyResolver.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Services;

using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Resolves effective leave policy by walking Global → Employee hierarchy.
/// Each level can override specific fields (entitlement wins at most-specific non-null level).
/// </summary>
public sealed class LeavePolicyResolver(TimeAttendanceDbContext db) : ILeavePolicyResolver
{
    private readonly TimeAttendanceDbContext _db = db;

    public async Task<LeavePolicyResolution?> ResolveAsync(
        Guid employeeId,
        Guid leaveTypeId,
        DateOnly forDate,
        CancellationToken ct = default)
    {
        // Check employee-level override first (most specific)
        EmployeeLeavePolicy? assignment = await _db.EmployeeLeavePolicies
            .Where(ep => ep.EmployeeId == employeeId
                && ep.LeaveTypeId == leaveTypeId
                && ep.IsActive
                && ep.EffectiveFrom <= forDate
                && (ep.EffectiveTo == null || ep.EffectiveTo >= forDate))
            .OrderByDescending(ep => ep.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (assignment is not null)
        {
            LeavePolicy? policy = await _db.LeavePolicies
                .FirstOrDefaultAsync(p => p.Id == assignment.ResolvedPolicyId && p.IsActive, ct);

            if (policy is not null)
            {
                return new LeavePolicyResolution
                {
                    WinningPolicyId = policy.Id,
                    WinningLevel = policy.Level,
                    EffectiveRules = policy.Rules,
                    EffectiveEntitlement = assignment.ResolvedEntitlement
                };
            }
        }

        // Fall through hierarchy: find most-specific active policy for this leave type
        List<LeavePolicy> policies = await _db.LeavePolicies
            .Where(p => p.LeaveTypeId == leaveTypeId && p.IsActive)
            .ToListAsync(ct);

        if (policies.Count == 0) return null;

        // Take highest-specificity policy available (Employee > Grade > Dept > BU > Country > Global)
        LeavePolicy winner = policies
            .OrderByDescending(p => (int)p.Level)
            .First();

        return new LeavePolicyResolution
        {
            WinningPolicyId = winner.Id,
            WinningLevel = winner.Level,
            EffectiveRules = winner.Rules,
            EffectiveEntitlement = (decimal)winner.Rules.AnnualAllowance
        };
    }
}
