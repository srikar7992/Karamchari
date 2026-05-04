namespace Karamchari.Payroll.Services.Statutory.Rules;

/// <summary>
/// Implements the Employee State Insurance (ESIC) deduction rule.
/// </summary>
public sealed class EsicStatutoryRule : IStatutoryRule
{
    private const decimal EsicWageThreshold = 21000m;
    private const decimal EsicRate = 0.0075m; // 0.75%

    /// <inheritdoc/>
    public string Name => "ESIC_Employee";

    /// <inheritdoc/>
    public int ExecutionOrder => 20;

    /// <inheritdoc/>
    public StatutoryResult Apply(StatutoryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // ESIC is typically calculated on the full Monthly Gross Pay
        decimal esicGross = context.Breakdown.MonthlyGross;

        // Applicability: Under 21k threshold OR locked into the contribution period
        bool isApplicable = esicGross <= EsicWageThreshold || context.Profile.IsEsicLocked;

        if (!isApplicable)
        {
            return new StatutoryResult(Name, 0m, false, "Gross salary exceeds threshold (₹21,000) and not period-locked.");
        }

        // ESIC rounding rule: Round UP to the next higher rupee (Ceil)
        decimal esicDeduction = Math.Ceiling(esicGross * EsicRate);

        return new StatutoryResult(Name, esicDeduction, true, "Standard 0.75% employee contribution applied.");
    }
}
