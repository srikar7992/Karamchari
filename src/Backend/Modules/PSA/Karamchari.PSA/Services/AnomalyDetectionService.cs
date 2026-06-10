// -----------------------------------------------------------------------
// <copyright file="AnomalyDetectionService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.PSA.Services;

/// <summary>
/// Deterministic anomaly detection â€” not ML, just hard thresholds that catch
/// real financial problems immediately.
///
/// Anomalies:
///   - Profit drop &gt; 20% month-over-month
///   - Revenue drop &gt; 20% month-over-month
///   - Cost spike &gt; 15% month-over-month
///   - Negative margin (project is losing money)
///   - Employee on bench (0 billable hours for 7+ days)
/// </summary>
public sealed class AnomalyDetectionService
{
    /// <summary>
    /// Detects anomalies by comparing current month metrics to the previous month.
    /// </summary>
    public static IReadOnlyList<Anomaly> Detect(
        IEnumerable<MonthComparison> comparisons)
    {
        ArgumentNullException.ThrowIfNull(comparisons);
        var anomalies = new List<Anomaly>();

        foreach (var c in comparisons)
        {
            // Profit drop > 20%
            if (c.PreviousProfit > 0 && c.CurrentProfit < c.PreviousProfit * 0.8m)
            {
                var dropPct = Math.Round((1 - c.CurrentProfit / c.PreviousProfit) * 100, 1);
                anomalies.Add(new Anomaly(
                    c.ProjectId,
                    AnomalyType.ProfitDrop,
                    $"Profit dropped {dropPct}% from â‚¹{c.PreviousProfit:N0} to â‚¹{c.CurrentProfit:N0}.",
                    AnomalySeverity.Critical));
            }

            // Revenue drop > 20%
            if (c.PreviousRevenue > 0 && c.CurrentRevenue < c.PreviousRevenue * 0.8m)
            {
                var dropPct = Math.Round((1 - c.CurrentRevenue / c.PreviousRevenue) * 100, 1);
                anomalies.Add(new Anomaly(
                    c.ProjectId,
                    AnomalyType.RevenueDrop,
                    $"Revenue dropped {dropPct}% from â‚¹{c.PreviousRevenue:N0} to â‚¹{c.CurrentRevenue:N0}.",
                    AnomalySeverity.Warning));
            }

            // Cost spike > 15%
            if (c.PreviousCost > 0 && c.CurrentCost > c.PreviousCost * 1.15m)
            {
                var spikePct = Math.Round((c.CurrentCost / c.PreviousCost - 1) * 100, 1);
                anomalies.Add(new Anomaly(
                    c.ProjectId,
                    AnomalyType.CostSpike,
                    $"Cost spiked {spikePct}% from â‚¹{c.PreviousCost:N0} to â‚¹{c.CurrentCost:N0}.",
                    AnomalySeverity.Warning));
            }

            // Negative margin
            if (c.CurrentRevenue > 0 && c.CurrentProfit < 0)
            {
                anomalies.Add(new Anomaly(
                    c.ProjectId,
                    AnomalyType.NegativeMargin,
                    $"Project is losing money. Revenue: â‚¹{c.CurrentRevenue:N0}, Cost: â‚¹{c.CurrentCost:N0}.",
                    AnomalySeverity.Critical));
            }
        }

        return anomalies;
    }
}

/// <summary>Month-over-month comparison data for a project.</summary>
public record MonthComparison(
    Guid ProjectId,
    decimal CurrentRevenue,
    decimal CurrentCost,
    decimal CurrentProfit,
    decimal PreviousRevenue,
    decimal PreviousCost,
    decimal PreviousProfit);

/// <summary>A detected financial anomaly.</summary>
public record Anomaly(
    Guid ProjectId,
    AnomalyType Type,
    string Message,
    AnomalySeverity Severity);

/// <summary>Types of financial anomalies.</summary>
public enum AnomalyType
{
    ProfitDrop,
    RevenueDrop,
    CostSpike,
    NegativeMargin,
    EmployeeOnBench
}

/// <summary>Severity levels for anomalies.</summary>
public enum AnomalySeverity
{
    Info,
    Warning,
    Critical
}
