// -----------------------------------------------------------------------
// <copyright file="HotspotCalculator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Intelligence.Domain.Workforce;

namespace Karamchari.Intelligence.Services.Scoring;

/// <param name="BurnoutScore">Employee burnout score [0,100].</param>
/// <param name="AttritionScore">Employee attrition risk score [0,100].</param>
public sealed record EmployeeScorePair(decimal BurnoutScore, decimal AttritionScore);

/// <summary>Output of <see cref="HotspotCalculator.Calculate"/>.</summary>
public sealed record HotspotResult(
    int TotalEmployees,
    decimal MeanBurnoutScore,
    decimal MeanAttritionScore,
    decimal BurnoutStdDev,
    decimal AttritionStdDev,
    int HighRiskBurnoutCount,
    int CriticalRiskBurnoutCount,
    int HighRiskAttritionCount,
    int CriticalRiskAttritionCount,
    HotspotSeverity BurnoutSeverity,
    HotspotSeverity AttritionSeverity);

/// <summary>
/// Pure static hotspot calculator.
/// Aggregates a list of employee score pairs into org-level statistics.
/// No I/O, no dependencies — directly unit-testable.
/// </summary>
public static class HotspotCalculator
{
    // Thresholds matching WorkforceRiskLevel
    private const decimal HighThreshold = 60m;
    private const decimal CriticalThreshold = 80m;

    // Severity classification
    private const decimal ElevatedThreshold = 40m;
    private const decimal CriticalMeanThreshold = 65m;

    /// <summary>
    /// Calculates aggregate hotspot statistics from a list of employee score pairs.
    /// Returns a zeroed result for empty input rather than throwing.
    /// </summary>
    public static HotspotResult Calculate(IReadOnlyList<EmployeeScorePair> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);

        if (scores.Count == 0)
        {
            return new HotspotResult(
                TotalEmployees: 0,
                MeanBurnoutScore: 0m,
                MeanAttritionScore: 0m,
                BurnoutStdDev: 0m,
                AttritionStdDev: 0m,
                HighRiskBurnoutCount: 0,
                CriticalRiskBurnoutCount: 0,
                HighRiskAttritionCount: 0,
                CriticalRiskAttritionCount: 0,
                BurnoutSeverity: HotspotSeverity.Normal,
                AttritionSeverity: HotspotSeverity.Normal);
        }

        var n = scores.Count;

        var meanBurnout = scores.Average(s => s.BurnoutScore);
        var meanAttrition = scores.Average(s => s.AttritionScore);

        var burnoutStdDev = StdDev(scores.Select(s => s.BurnoutScore), meanBurnout);
        var attritionStdDev = StdDev(scores.Select(s => s.AttritionScore), meanAttrition);

        var highBurnout = scores.Count(s => s.BurnoutScore >= HighThreshold && s.BurnoutScore < CriticalThreshold);
        var criticalBurnout = scores.Count(s => s.BurnoutScore >= CriticalThreshold);
        var highAttrition = scores.Count(s => s.AttritionScore >= HighThreshold && s.AttritionScore < CriticalThreshold);
        var criticalAttrition = scores.Count(s => s.AttritionScore >= CriticalThreshold);

        return new HotspotResult(
            TotalEmployees: n,
            MeanBurnoutScore: Math.Round(meanBurnout, 1),
            MeanAttritionScore: Math.Round(meanAttrition, 1),
            BurnoutStdDev: Math.Round(burnoutStdDev, 1),
            AttritionStdDev: Math.Round(attritionStdDev, 1),
            HighRiskBurnoutCount: highBurnout,
            CriticalRiskBurnoutCount: criticalBurnout,
            HighRiskAttritionCount: highAttrition,
            CriticalRiskAttritionCount: criticalAttrition,
            BurnoutSeverity: Classify(meanBurnout),
            AttritionSeverity: Classify(meanAttrition));
    }

    private static decimal StdDev(IEnumerable<decimal> values, decimal mean)
    {
        var list = values.ToList();
        if (list.Count < 2) return 0m;
        var sumSquares = list.Sum(v => (v - mean) * (v - mean));
        return (decimal)Math.Sqrt((double)(sumSquares / list.Count));
    }

    private static HotspotSeverity Classify(decimal mean) => mean switch
    {
        < ElevatedThreshold => HotspotSeverity.Normal,
        < CriticalMeanThreshold => HotspotSeverity.Elevated,
        _ => HotspotSeverity.Critical
    };
}
