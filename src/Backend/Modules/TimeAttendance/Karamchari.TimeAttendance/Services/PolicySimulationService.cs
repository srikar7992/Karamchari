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
        var assignments = await db.EmployeeLeavePolicies
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
        var balances = await db.LeaveBalances
            .Where(b => b.TenantId == request.TenantId
                     && employeeIds.Contains(b.EmployeeId))
            .ToDictionaryAsync(b => b.EmployeeId, ct);

        // Active carry forwards
        var carryForwards = await db.LeaveCarryForwards
            .Where(cf => cf.TenantId == request.TenantId
                      && employeeIds.Contains(cf.EmployeeId))
            .GroupBy(cf => cf.EmployeeId)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(cf => cf.CarriedDays), ct);

        var impacts = new List<PolicySimulationEmployeeImpact>(assignments.Count);
        decimal totalCurrentCarryForward = 0;
        decimal totalEstimatedCarryForward = 0;

        foreach (var assignment in assignments)
        {
            var currentBalance = balances.TryGetValue(assignment.EmployeeId, out var b)
                ? b.AvailableBalance
                : 0m;

            var currentEntitlement = assignment.ResolvedEntitlement;
            var proposedEntitlement = (decimal)request.ProposedRules.AnnualAllowance;
            var entitlementDelta = proposedEntitlement - currentEntitlement;

            var projectedBalance = currentBalance + entitlementDelta;

            // Carry forward under proposed rules
            var currentCf = carryForwards.TryGetValue(assignment.EmployeeId, out var cf) ? cf : 0m;
            totalCurrentCarryForward += currentCf;

            var maxCf = (decimal)request.ProposedRules.MaximumCarryForward;
            var estimatedCf = Math.Min(projectedBalance, maxCf);
            totalEstimatedCarryForward += estimatedCf;

            var summary = entitlementDelta switch
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

        var avgCurrentEntitlement = assignments.Average(a => a.ResolvedEntitlement);
        var avgProposedEntitlement = (decimal)request.ProposedRules.AnnualAllowance;
        var financialImpactDays = impacts.Sum(i => Math.Max(0, i.BalanceDelta));

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
