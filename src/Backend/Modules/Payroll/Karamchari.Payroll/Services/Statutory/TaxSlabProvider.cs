// -----------------------------------------------------------------------
// <copyright file="TaxSlabProvider.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Services.Statutory;

using Karamchari.Payroll.Domain.Statutory;

/// <summary>
/// Calculates annual tax liability based on Indian tax slabs.
/// </summary>
public sealed class TaxSlabProvider : ITaxSlabProvider
{
    /// <inheritdoc/>
    public decimal CalculateAnnualTax(decimal taxableIncome, TaxRegime regime)
    {
        if (taxableIncome <= 0) return 0;

        decimal tax = regime switch
        {
            TaxRegime.Old => CalculateOldRegimeTax(taxableIncome),
            TaxRegime.New => CalculateNewRegimeTax(taxableIncome),
            _ => CalculateNewRegimeTax(taxableIncome)
        };

        // Add Health and Education Cess (4%)
        return Math.Round(tax * 1.04m, 0, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateOldRegimeTax(decimal income)
    {
        // 87A Rebate for Old Regime (up to 5L taxable income)
        if (income <= 500000) return 0;

        decimal tax = 0;

        // 2.5L to 5L @ 5%
        if (income > 250000)
            tax += (Math.Min(income, 500000) - 250000) * 0.05m;

        // 5L to 10L @ 20%
        if (income > 500000)
            tax += (Math.Min(income, 1000000) - 500000) * 0.20m;

        // Above 10L @ 30%
        if (income > 1000000)
            tax += (income - 1000000) * 0.30m;

        return tax;
    }

    private static decimal CalculateNewRegimeTax(decimal income)
    {
        // 87A Rebate for New Regime (up to 7L taxable income)
        if (income <= 700000) return 0;

        decimal tax = 0;

        // 0-3L: Nil
        // 3-7L: 5%
        if (income > 300000)
            tax += (Math.Min(income, 700000) - 300000) * 0.05m;

        // 7-10L: 10%
        if (income > 700000)
            tax += (Math.Min(income, 1000000) - 700000) * 0.10m;

        // 10-12L: 15%
        if (income > 1000000)
            tax += (Math.Min(income, 1200000) - 1000000) * 0.15m;

        // 12-15L: 20%
        if (income > 1200000)
            tax += (Math.Min(income, 1500000) - 1200000) * 0.20m;

        // Above 15L: 30%
        if (income > 1500000)
            tax += (income - 1500000) * 0.30m;

        return tax;
    }
}
