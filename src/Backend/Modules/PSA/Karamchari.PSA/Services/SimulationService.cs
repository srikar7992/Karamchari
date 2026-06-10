// -----------------------------------------------------------------------
// <copyright file="SimulationService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.PSA.Services;

/// <summary>
/// Ephemeral what-if engine. Takes current pre-aggregated metrics and projects
/// the impact of rate/cost changes WITHOUT persisting anything.
///
/// Critical rule: simulation data is NEVER persisted. It's a read-only projection.
/// </summary>
public sealed class SimulationService
{
    /// <summary>
    /// Simulates the impact of rate and cost changes on a project's financials.
    /// </summary>
    /// <param name="currentRevenue">Current total revenue.</param>
    /// <param name="currentCost">Current total cost.</param>
    /// <param name="rateChangePercent">Percentage change in billing rate (e.g., 10 for +10%).</param>
    /// <param name="costChangePercent">Percentage change in employee cost (e.g., 5 for +5%).</param>
    public static SimulationResult Simulate(
        decimal currentRevenue,
        decimal currentCost,
        decimal rateChangePercent = 0,
        decimal costChangePercent = 0)
    {
        decimal newRevenue = Math.Round(currentRevenue * (1 + rateChangePercent / 100), 2);
        decimal newCost = Math.Round(currentCost * (1 + costChangePercent / 100), 2);
        decimal newProfit = newRevenue - newCost;
        decimal newMargin = newRevenue == 0 ? 0 : Math.Round(newProfit / newRevenue * 100, 2);

        decimal originalProfit = currentRevenue - currentCost;
        decimal originalMargin = currentRevenue == 0 ? 0 : Math.Round(originalProfit / currentRevenue * 100, 2);

        return new SimulationResult(
            OriginalRevenue: currentRevenue,
            OriginalCost: currentCost,
            OriginalProfit: originalProfit,
            OriginalMargin: originalMargin,
            SimulatedRevenue: newRevenue,
            SimulatedCost: newCost,
            SimulatedProfit: newProfit,
            SimulatedMargin: newMargin,
            ProfitDelta: Math.Round(newProfit - originalProfit, 2),
            MarginDelta: Math.Round(newMargin - originalMargin, 2));
    }
}

/// <summary>
/// Simulation result comparing original vs projected financials.
/// </summary>
public record SimulationResult(
    decimal OriginalRevenue,
    decimal OriginalCost,
    decimal OriginalProfit,
    decimal OriginalMargin,
    decimal SimulatedRevenue,
    decimal SimulatedCost,
    decimal SimulatedProfit,
    decimal SimulatedMargin,
    decimal ProfitDelta,
    decimal MarginDelta);
