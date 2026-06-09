using System;
using System.Collections.Generic;
using Karamchari.HR.Domain.Organization;
using Karamchari.HR.Domain.Organization.Projections;

namespace Karamchari.HR.Services;

/// <summary>
/// Provides pure calculations for determining an employee's readiness for promotion.
/// </summary>
public static class PromotionReadinessCalculator
{
    /// <summary>
    /// Calculates the promotion readiness score and determines driver factors for an employee.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant context.</param>
    /// <param name="employeeId">The unique identifier of the employee.</param>
    /// <param name="monthsInRole">The number of months the employee has spent in their current role, if available.</param>
    /// <param name="skillCount">The count of verified skills held by the employee.</param>
    /// <param name="perf">The employee's performance snapshot projection, if available.</param>
    /// <returns>An <see cref="EmployeePromotionReadiness"/> containing the calculated readiness score and contributing drivers.</returns>
    public static EmployeePromotionReadiness Calculate(
        string tenantId,
        Guid employeeId,
        double? monthsInRole,
        int skillCount,
        EmployeePerformanceProjection? perf)
    {
        var drivers = new List<PromotionReadinessDriverProjection>();
        decimal totalScore = 0.10m; // Base readiness score

        if (monthsInRole.HasValue && monthsInRole.Value > 12)
        {
            var factorScore = 0.35m;
            drivers.Add(new PromotionReadinessDriverProjection
            {
                TenantId = tenantId,
                EmployeeId = employeeId,
                DriverType = PromotionReadinessDriverType.RoleTenureMilestone,
                FactorScore = factorScore,
                Explanation = $"Tenure in role is {monthsInRole.Value:F1} months, exceeding the target development window of 12 months."
            });
            totalScore += factorScore;
        }

        if (skillCount >= 3)
        {
            var factorScore = 0.25m;
            drivers.Add(new PromotionReadinessDriverProjection
            {
                TenantId = tenantId,
                EmployeeId = employeeId,
                DriverType = PromotionReadinessDriverType.SkillMilestonesMet,
                FactorScore = factorScore,
                Explanation = $"Employee holds {skillCount} verified skills, meeting or exceeding the skill benchmark."
            });
            totalScore += factorScore;
        }

        if (perf != null && perf.IsHighPerformer)
        {
            var factorScore = 0.25m;
            drivers.Add(new PromotionReadinessDriverProjection
            {
                TenantId = tenantId,
                EmployeeId = employeeId,
                DriverType = PromotionReadinessDriverType.HighGoalVelocity,
                FactorScore = factorScore,
                Explanation = $"Employee has been calibrated as a high performer in the recent cycle with score of {perf.CompositeScore:F2}."
            });
            totalScore += factorScore;
        }

        return new EmployeePromotionReadiness
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            ReadinessScore = Math.Min(totalScore, 0.95m),
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            Drivers = drivers
        };
    }
}
