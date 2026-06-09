using System;

namespace Karamchari.Capability.Intelligence;

/// <summary>
/// Domain service for calculating employee retention risk based on multi-variable organization telemetry.
/// </summary>
public sealed class WorkforceRiskEngine
{
    /// <summary>
    /// Predicts retention flight risk probability as a decimal between 0.0 and 1.0.
    /// </summary>
    /// <param name="performanceRating">Employee rating value (typically 1 to 5).</param>
    /// <param name="ratioToMarketMedian">Salary ratio comparison factor (e.g. 0.85 for 15% below market median).</param>
    /// <param name="promotionMonthsDelay">Months elapsed since last job grade promotion.</param>
    /// <param name="managerTurnoverRate">Historic department manager turnover rate.</param>
    /// <returns>A flight risk percentage probability.</returns>
    public static decimal PredictRetentionRisk(
        int performanceRating,
        decimal ratioToMarketMedian,
        int promotionMonthsDelay,
        decimal managerTurnoverRate)
    {
        decimal risk = 0.10m; // Baseline friction factor

        // High performance coupled with low market positioning increases flight risk
        if (performanceRating >= 4 && ratioToMarketMedian < 0.90m)
        {
            risk += 0.40m;
        }

        // Stagnation over 2 years in same title level increases risk
        if (promotionMonthsDelay > 24)
        {
            risk += 0.20m;
        }

        // Department manager churn indicates unstable leadership structure
        if (managerTurnoverRate > 0.30m)
        {
            risk += 0.15m;
        }

        // Bound probability ceiling
        return Math.Min(risk, 0.95m);
    }
}
