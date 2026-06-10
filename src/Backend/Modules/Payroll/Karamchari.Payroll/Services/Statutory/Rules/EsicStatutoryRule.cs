// -----------------------------------------------------------------------
// <copyright file="EsicStatutoryRule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
            return new StatutoryResult(Name, 0m, false, $"Gross salary (â‚¹{esicGross:N2}) exceeds threshold (â‚¹{EsicWageThreshold:N2}) and not period-locked.");
        }

        // ESIC rounding rule: Round UP to the next higher rupee (Ceil)
        decimal esicDeduction = Math.Ceiling(esicGross * EsicRate);
        string lockNote = context.Profile.IsEsicLocked ? " (Period Locked)" : string.Empty;

        return new StatutoryResult(Name, esicDeduction, true, $"0.75% of Gross (â‚¹{esicGross:N2}){lockNote}");
    }
}
