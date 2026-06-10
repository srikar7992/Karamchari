// -----------------------------------------------------------------------
// <copyright file="CTCBreakdownService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Services;

using Karamchari.Payroll.Domain.SalaryStructures;

/// <summary>
/// Represents the result of a CTC breakdown calculation.
/// </summary>
/// <param name="AnnualCTC">The total annual cost to company.</param>
/// <param name="MonthlyGross">The derived monthly gross salary.</param>
/// <param name="AnnualBreakdown">The annual values per component.</param>
/// <param name="MonthlyBreakdown">The monthly values per component.</param>
public record CTCBreakdownResult(
    decimal AnnualCTC,
    decimal MonthlyGross,
    Dictionary<Guid, decimal> AnnualBreakdown,
    Dictionary<Guid, decimal> MonthlyBreakdown
);

/// <summary>
/// Executes the mathematical breakdown of a CTC based on a compiled execution plan.
/// </summary>
public sealed class CTCBreakdownService
{
    /// <summary>
    /// Calculates the breakdown.
    /// </summary>
    public static CTCBreakdownResult Calculate(decimal annualCTC, CompiledExecutionPlan plan, Dictionary<Guid, decimal>? overrides = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var computedAnnual = new Dictionary<Guid, decimal>();
        overrides ??= new Dictionary<Guid, decimal>();

        // 1. Pre-load explicit HR overrides
        foreach (var over in overrides)
        {
            computedAnnual[over.Key] = over.Value;
        }

        // 2. Execute standard DAG steps
        foreach (var step in plan.OrderedSteps)
        {
            if (computedAnnual.ContainsKey(step.ComponentId)) continue;

            decimal value = 0m;
            switch (step.CalcType)
            {
                case ComponentCalculationType.FixedAmount:
                    value = step.Value ?? 0;
                    break;
                case ComponentCalculationType.PercentageOfCTC:
                    value = annualCTC * ((step.Value ?? 0) / 100m);
                    break;
                case ComponentCalculationType.PercentageOfComponents:
                    decimal baseAmount = 0m;
                    if (step.Aggregation == DependencyAggregationType.Sum)
                    {
                        baseAmount = step.Dependencies.Sum(d => computedAnnual.GetValueOrDefault(d, 0));
                    }
                    else if (step.Aggregation == DependencyAggregationType.Max)
                    {
                        baseAmount = step.Dependencies.Count > 0
                            ? step.Dependencies.Max(d => computedAnnual.GetValueOrDefault(d, 0))
                            : 0;
                    }
                    value = baseAmount * ((step.Value ?? 0) / 100m);
                    break;
            }
            computedAnnual[step.ComponentId] = ApplyRounding(value, step.Rounding);
        }

        // 3. Compute Remainder (Special Allowance Balancer)
        if (plan.RemainderStep != null)
        {
            if (!computedAnnual.ContainsKey(plan.RemainderStep.ComponentId))
            {
                // Remainder = CTC - SUM(Earnings + Employer Contributions)
                decimal totalAllocated = 0m;

                foreach (var step in plan.OrderedSteps)
                {
                    if (step.Type == ComponentType.Earning || step.Type == ComponentType.EmployerContribution)
                    {
                        totalAllocated += computedAnnual[step.ComponentId];
                    }
                }

                decimal remainderValue = annualCTC - totalAllocated;

                if (remainderValue < 0)
                {
                    throw new InvalidOperationException($"CTC breakdown overflow. Allocated {totalAllocated} exceeds CTC {annualCTC}.");
                }

                computedAnnual[plan.RemainderStep.ComponentId] = ApplyRounding(remainderValue, plan.RemainderStep.Rounding);
            }
        }

        // 4. Derive Monthly values and Gross
        decimal monthlyGross = 0m;
        var computedMonthly = new Dictionary<Guid, decimal>();
        var allSteps = plan.OrderedSteps.ToList();
        if (plan.RemainderStep != null) allSteps.Add(plan.RemainderStep);

        foreach (var kvp in computedAnnual)
        {
            // Standard monthly rounding to nearest rupee
            var monthlyVal = Math.Round(kvp.Value / 12m, 0, MidpointRounding.AwayFromZero);
            computedMonthly[kvp.Key] = monthlyVal;

            var step = allSteps.First(s => s.ComponentId == kvp.Key);
            if (step.Type == ComponentType.Earning)
            {
                monthlyGross += monthlyVal;
            }
        }

        return new CTCBreakdownResult(annualCTC, monthlyGross, computedAnnual, computedMonthly);
    }

    private static decimal ApplyRounding(decimal value, RoundingStrategy strategy) => strategy switch
    {
        RoundingStrategy.Floor => Math.Floor(value),
        RoundingStrategy.Ceil => Math.Ceiling(value),
        RoundingStrategy.NearestRupee => Math.Round(value, 0, MidpointRounding.AwayFromZero),
        RoundingStrategy.Exact => value,
        _ => value
    };
}
