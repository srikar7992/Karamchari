namespace Karamchari.Payroll.Services.Calculation;

/// <summary>
/// Slab-based Indian income tax engine.
/// Supports Old and New regimes (FY 2024-25 defaults).
/// TDS this month = (projected annual tax - YTD TDS already deducted) / remaining months.
/// Deterministic: same inputs always produce same output.
/// </summary>
public sealed class TaxCalculatorService : ITaxCalculatorService
{
    // New Regime slabs FY 2024-25 (Budget 2024): 0-3L=0%, 3-7L=5%, 7-10L=10%, 10-12L=15%, 12-15L=20%, 15L+=30%
    private static readonly (decimal UpTo, decimal Rate)[] NewRegimeSlabs =
    [
        (300_000m, 0.00m),
        (700_000m, 0.05m),
        (1_000_000m, 0.10m),
        (1_200_000m, 0.15m),
        (1_500_000m, 0.20m),
        (decimal.MaxValue, 0.30m),
    ];

    // Old Regime slabs: 0-2.5L=0%, 2.5-5L=5%, 5-10L=20%, 10L+=30%
    private static readonly (decimal UpTo, decimal Rate)[] OldRegimeSlabs =
    [
        (250_000m, 0.00m),
        (500_000m, 0.05m),
        (1_000_000m, 0.20m),
        (decimal.MaxValue, 0.30m),
    ];

    // Surcharge thresholds (both regimes)
    private static decimal ApplySurcharge(decimal tax, decimal taxableIncome) =>
        taxableIncome switch
        {
            > 50_000_000m => tax * 1.37m,  // 37% surcharge
            > 20_000_000m => tax * 1.25m,  // 25%
            > 10_000_000m => tax * 1.15m,  // 15%
            > 5_000_000m => tax * 1.10m,   // 10%
            _ => tax,
        };

    public TaxCalculationOutput Calculate(TaxCalculationInput input)
    {
        var isNewRegime = !string.Equals(input.TaxRegime, "OLD", StringComparison.OrdinalIgnoreCase);
        var slabs = isNewRegime ? NewRegimeSlabs : OldRegimeSlabs;

        // Standard deduction: 75k (new regime), 50k (old)
        var standardDeduction = isNewRegime ? 75_000m : 50_000m;

        // Annualize from current YTD + current month projection
        var monthsElapsed = FyMonthsElapsed(input.FinancialYear, input.Month);
        var remainingMonths = Math.Max(0, 12 - monthsElapsed);

        var projectedAnnualGross = input.YearToDateGross + input.CurrentMonthGross
            + input.CurrentMonthGross * remainingMonths;

        // Old regime deductions: 80C (PF + declared investments, capped ₹1.5L), 80D, PT
        decimal section80C = 0m;
        decimal ptDeduction = 0m;
        if (!isNewRegime)
        {
            var pfAnnualised = input.PfDeduction * 12m;
            section80C = Math.Min(input.DeclaredInvestments + pfAnnualised, 150_000m);
            ptDeduction = input.ProfessionalTax * 12m;
        }

        var taxableIncome = Math.Max(0m,
            projectedAnnualGross - standardDeduction - section80C - ptDeduction);

        var rawTax = CalculateSlabTax(taxableIncome, slabs);
        var taxWithSurcharge = ApplySurcharge(rawTax, taxableIncome);

        // Rebate u/s 87A
        var (rebateThreshold, rebateMax) = isNewRegime
            ? (700_000m, 25_000m)
            : (500_000m, 12_500m);

        if (taxableIncome <= rebateThreshold)
            taxWithSurcharge = Math.Max(0m, taxWithSurcharge - rebateMax);

        // Health & Education Cess 4%
        var cess = Math.Round(taxWithSurcharge * 0.04m, 0);
        var annualTaxLiability = Math.Round(taxWithSurcharge + cess, 0);

        // TDS this month: spread remainder evenly over remaining months (including current)
        var monthsLeft = remainingMonths + 1; // include current month
        var tdsThisMonth = Math.Max(0m,
            Math.Round((annualTaxLiability - input.YearToDateTds) / monthsLeft, 0));

        return new TaxCalculationOutput(taxableIncome, annualTaxLiability, tdsThisMonth, "FY2024-25-V1");
    }

    private static decimal CalculateSlabTax(decimal income, (decimal UpTo, decimal Rate)[] slabs)
    {
        var tax = 0m;
        var prev = 0m;
        foreach (var (upTo, rate) in slabs)
        {
            if (income <= prev) break;
            var taxable = Math.Min(income, upTo == decimal.MaxValue ? income : upTo) - prev;
            tax += Math.Round(taxable * rate, 0);
            prev = upTo == decimal.MaxValue ? income : upTo;
        }
        return tax;
    }

    // FY starts April. Returns months elapsed including current month.
    private static int FyMonthsElapsed(int financialYear, int month)
    {
        // FY 2024-25: Apr 2024 = month 1 of FY, Mar 2025 = month 12
        return month >= 4 ? month - 3 : month + 9;
    }
}
