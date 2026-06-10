// -----------------------------------------------------------------------
// <copyright file="WorkloadFairnessCalculator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Intelligence.Domain.Workforce;

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Measures how equitably overtime and shift duties are distributed across a team.
/// Uses the Gini coefficient as the primary fairness metric.
///
/// Score = 100 × (1 − gini).
/// Perfect equality → gini = 0 → score = 100.
/// One person does all OT → gini → 1 → score → 0.
/// </summary>
public static class WorkloadFairnessCalculator
{
    /// <summary>
    /// Calculates workload fairness for a team given each member's OT hours.
    /// </summary>
    public static WorkloadFairnessResult Calculate(WorkloadFairnessInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.MemberOtHours.Count == 0)
            return new WorkloadFairnessResult(100m, 0m, 0m, 0, 0m, 0m);

        var sorted = input.MemberOtHours.OrderBy(x => x).ToArray();
        int n = sorted.Length;

        var total = sorted.Sum();
        var avg = n == 0 ? 0m : total / n;
        var max = sorted[^1];

        // Gini = sum of absolute differences / (2 * n * mean)
        decimal gini;
        if (avg == 0m || n < 2)
        {
            gini = 0m;
        }
        else
        {
            decimal absDiffSum = 0m;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    absDiffSum += Math.Abs(sorted[i] - sorted[j]);
            gini = absDiffSum / (2m * n * avg);
        }

        gini = Math.Clamp(Math.Round(gini, 4), 0m, 1m);

        // Top 10% concentration
        int top10Count = Math.Max(1, (int)Math.Ceiling(n * 0.10));
        var top10Hours = sorted[^top10Count..].Sum();
        var concentration = total > 0m ? top10Hours / total : 0m;

        var score = Math.Clamp(Math.Round(100m * (1m - gini), 1), 0m, 100m);

        return new WorkloadFairnessResult(
            OverallScore: score,
            OtGiniCoefficient: gini,
            OtConcentrationTop10Pct: Math.Round(concentration, 4),
            TeamSize: n,
            AvgOtHoursPerMember: Math.Round(avg, 2),
            MaxOtHoursAnyMember: max);
    }
}

/// <summary>Input for WorkloadFairnessCalculator.</summary>
public sealed record WorkloadFairnessInput(
    IReadOnlyList<decimal> MemberOtHours);

/// <summary>Output of WorkloadFairnessCalculator.Calculate.</summary>
public sealed record WorkloadFairnessResult(
    decimal OverallScore,
    decimal OtGiniCoefficient,
    decimal OtConcentrationTop10Pct,
    int TeamSize,
    decimal AvgOtHoursPerMember,
    decimal MaxOtHoursAnyMember);
