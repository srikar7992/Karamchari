namespace Karamchari.PSA.Services;

/// <summary>
/// Computes the optimal billing rate to achieve a target profit margin.
///
/// Formula: OptimalRate = CostPerHour / (1 âˆ’ TargetMargin)
///
/// Example: If CostPerHour = â‚¹625 and TargetMargin = 30%,
///          OptimalRate = 625 / 0.7 = â‚¹892.86/hr
///
/// This tells the CFO: "You need to charge at least â‚¹893/hr to achieve 30% margin."
/// </summary>
public sealed class PricingEngine
{
    /// <summary>
    /// Recommends an optimal billing rate based on employee cost and target margin.
    /// </summary>
    /// <param name="costPerHour">Fully-loaded cost per hour from EmployeeCostSnapshot.</param>
    /// <param name="currentRate">The rate currently charged to the client.</param>
    /// <param name="targetMargin">Target profit margin (0.30 = 30%).</param>
    public static PricingRecommendation Recommend(
        decimal costPerHour,
        decimal currentRate,
        decimal targetMargin = 0.30m)
    {
        if (targetMargin >= 1) throw new ArgumentOutOfRangeException(nameof(targetMargin), "Margin must be < 1.");

        decimal optimalRate = costPerHour <= 0 ? currentRate : Math.Round(costPerHour / (1 - targetMargin), 2);
        decimal marginAtCurrent = currentRate > 0 ? Math.Round((currentRate - costPerHour) / currentRate * 100, 2) : 0;

        return new PricingRecommendation(
            CurrentRate: currentRate,
            RecommendedRate: optimalRate,
            Delta: Math.Round(optimalRate - currentRate, 2),
            MarginAtCurrentRate: marginAtCurrent,
            MarginAtRecommendedRate: Math.Round(targetMargin * 100, 2),
            CostPerHour: costPerHour);
    }
}

/// <summary>
/// Pricing recommendation output.
/// </summary>
public record PricingRecommendation(
    decimal CurrentRate,
    decimal RecommendedRate,
    decimal Delta,
    decimal MarginAtCurrentRate,
    decimal MarginAtRecommendedRate,
    decimal CostPerHour);
