// -----------------------------------------------------------------------
// <copyright file="RetentionRiskCalculator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Karamchari.HR.Domain.Organization;
using Karamchari.HR.Domain.Organization.Projections;

namespace Karamchari.HR.Services;

/// <summary>
/// Provides pure calculations for determining an employee's retention/flight risk.
/// </summary>
public static class RetentionRiskCalculator
{
    /// <summary>
    /// Calculates the retention risk score and determines driver factors for an employee.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant context.</param>
    /// <param name="employeeId">The unique identifier of the employee.</param>
    /// <param name="monthsInRole">The number of months the employee has spent in their current role, if available.</param>
    /// <param name="monthsSinceManagerChange">The number of months since the employee's reporting manager last changed, if available.</param>
    /// <param name="comp">The employee's compensation details projection, if available.</param>
    /// <param name="perf">The employee's performance snapshot projection, if available.</param>
    /// <returns>An <see cref="EmployeeFlightRiskPrediction"/> containing the calculated score and contributing drivers.</returns>
    public static EmployeeFlightRiskPrediction Calculate(
        string tenantId,
        Guid employeeId,
        double? monthsInRole,
        double? monthsSinceManagerChange,
        EmployeeCompensationProjection? comp,
        EmployeePerformanceProjection? perf)
    {
        var drivers = new List<FlightRiskDriverProjection>();
        decimal totalScore = 0.05m; // Base friction score

        if (monthsInRole.HasValue && monthsInRole.Value > 24)
        {
            var factorScore = 0.30m;
            drivers.Add(new FlightRiskDriverProjection
            {
                TenantId = tenantId,
                EmployeeId = employeeId,
                DriverType = FlightRiskDriverType.HighTenureInRole,
                FactorScore = factorScore,
                Explanation = $"Employee has been in their current position for {monthsInRole.Value:F1} months, exceeding the high tenure threshold of 24 months."
            });
            totalScore += factorScore;
        }

        if (monthsSinceManagerChange.HasValue && monthsSinceManagerChange.Value < 6)
        {
            var factorScore = 0.20m;
            drivers.Add(new FlightRiskDriverProjection
            {
                TenantId = tenantId,
                EmployeeId = employeeId,
                DriverType = FlightRiskDriverType.RecentManagerChange,
                FactorScore = factorScore,
                Explanation = $"Employee experienced a manager change within the last {monthsSinceManagerChange.Value:F1} months."
            });
            totalScore += factorScore;
        }

        if (comp != null && comp.IncrementPercent < 0.03m)
        {
            var factorScore = 0.20m;
            drivers.Add(new FlightRiskDriverProjection
            {
                TenantId = tenantId,
                EmployeeId = employeeId,
                DriverType = FlightRiskDriverType.LowCompRange,
                FactorScore = factorScore,
                Explanation = $"Employee's last compensation increment was {comp.IncrementPercent * 100:F1}%, which is below the target merit threshold of 3%."
            });
            totalScore += factorScore;
        }

        if (perf != null && !perf.IsHighPerformer && perf.CompositeScore < 3.0m)
        {
            var factorScore = 0.20m;
            drivers.Add(new FlightRiskDriverProjection
            {
                TenantId = tenantId,
                EmployeeId = employeeId,
                DriverType = FlightRiskDriverType.LowPerformanceTrend,
                FactorScore = factorScore,
                Explanation = $"Employee's last calibrated performance rating was low ({perf.CompositeScore:F2}/5.00)."
            });
            totalScore += factorScore;
        }

        return new EmployeeFlightRiskPrediction
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            RiskScore = Math.Min(totalScore, 0.95m),
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            Drivers = drivers
        };
    }
}
