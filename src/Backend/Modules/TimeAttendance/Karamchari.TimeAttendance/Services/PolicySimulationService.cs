// -----------------------------------------------------------------------
// <copyright file="PolicySimulationService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Services;

public sealed record PolicySimulationRequest(
    string TenantId,
    Guid LeaveTypeId,
    LeavePolicyRules ProposedRules,
    int? TargetDepartmentId,
    DateOnly SimulationAsOf);

public sealed record PolicySimulationResult(
    int AffectedEmployeeCount,
    decimal CurrentAverageDaysEntitlement,
    decimal ProposedAverageDaysEntitlement,
    decimal CurrentTotalCarryForwardDays,
    decimal EstimatedNewCarryForwardDays,
    decimal EstimatedFinancialImpactDays,
    string CurrencyCode,
    IReadOnlyList<PolicySimulationEmployeeImpact> EmployeeImpacts);

public sealed record PolicySimulationEmployeeImpact(
    Guid EmployeeId,
    decimal CurrentBalance,
    decimal ProjectedBalance,
    decimal CurrentEntitlement,
    decimal ProposedEntitlement,
    decimal BalanceDelta,
    string ImpactSummary);

/// <summary>
/// Simulates the financial and balance impact of changing leave policy rules
/// before the policy is activated. Uses existing LeaveBalance snapshots and
/// EmployeeLeavePolicy records — no side effects, pure read + projection.
/// </summary>
public sealed class PolicySimulationService(TimeAttendanceDbContext db)
{
    public async Task<PolicySimulationResult> SimulateAsync(
        PolicySimulationRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // All active employee-policy assignments for this leave type
        List<EmployeeLeavePolicy> assignments = await db.EmployeeLeavePolicies
            .Where(p => p.TenantId == request.TenantId
                     && p.LeaveTypeId == request.LeaveTypeId
                     && p.IsActive
                     && p.EffectiveFrom <= request.SimulationAsOf)
            .ToListAsync(ct);

        if (assignments.Count == 0)
        {
            return new PolicySimulationResult(0, 0, 0, 0, 0, 0, "USD", []);
        }

        var employeeIds = assignments.Select(a => a.EmployeeId).ToList();

        // Current balances keyed by employeeId
        Dictionary<Guid, LeaveBalance> balances = await db.LeaveBalances
            .Where(b => b.TenantId == request.TenantId
                     && employeeIds.Contains(b.EmployeeId))
            .ToDictionaryAsync(b => b.EmployeeId, ct);

        // Active carry forwards
        Dictionary<Guid, decimal> carryForwards = await db.LeaveCarryForwards
            .Where(cf => cf.TenantId == request.TenantId
                      && employeeIds.Contains(cf.EmployeeId))
            .GroupBy(cf => cf.EmployeeId)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(cf => cf.CarriedDays), ct);

        var impacts = new List<PolicySimulationEmployeeImpact>(assignments.Count);
        decimal totalCurrentCarryForward = 0;
        decimal totalEstimatedCarryForward = 0;

        foreach (EmployeeLeavePolicy? assignment in assignments)
        {
            decimal currentBalance = balances.TryGetValue(assignment.EmployeeId, out LeaveBalance? b)
                ? b.AvailableBalance
                : 0m;

            decimal currentEntitlement = assignment.ResolvedEntitlement;
            decimal proposedEntitlement = (decimal)request.ProposedRules.AnnualAllowance;
            decimal entitlementDelta = proposedEntitlement - currentEntitlement;

            decimal projectedBalance = currentBalance + entitlementDelta;

            // Carry forward under proposed rules
            decimal currentCf = carryForwards.TryGetValue(assignment.EmployeeId, out decimal cf) ? cf : 0m;
            totalCurrentCarryForward += currentCf;

            decimal maxCf = (decimal)request.ProposedRules.MaximumCarryForward;
            decimal estimatedCf = Math.Min(projectedBalance, maxCf);
            totalEstimatedCarryForward += estimatedCf;

            string summary = entitlementDelta switch
            {
                > 0 => $"+{entitlementDelta:F1} days entitlement increase",
                < 0 => $"{entitlementDelta:F1} days entitlement decrease",
                _ => "No entitlement change",
            };

            impacts.Add(new PolicySimulationEmployeeImpact(
                assignment.EmployeeId,
                currentBalance,
                projectedBalance,
                currentEntitlement,
                proposedEntitlement,
                entitlementDelta,
                summary));
        }

        decimal avgCurrentEntitlement = assignments.Average(a => a.ResolvedEntitlement);
        decimal avgProposedEntitlement = (decimal)request.ProposedRules.AnnualAllowance;
        decimal financialImpactDays = impacts.Sum(i => Math.Max(0, i.BalanceDelta));

        return new PolicySimulationResult(
            assignments.Count,
            avgCurrentEntitlement,
            avgProposedEntitlement,
            totalCurrentCarryForward,
            totalEstimatedCarryForward,
            financialImpactDays,
            "USD",
            impacts);
    }
}
