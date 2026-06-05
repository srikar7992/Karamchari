using Karamchari.Payroll.Services.Calculation;

namespace Karamchari.Payroll.Tests.Phase4;

/// <summary>
/// Compliance matrix for TaxCalculatorService (FY 2024-25).
/// Each test asserts exact annual tax liability and/or TDS per month.
/// Calculations verified against CBDT slab tables.
///
/// Convention: month=3 (March) + YtdGross=0 collapses projectedAnnual = CurrentMonthGross,
/// isolating the slab math from the TDS spread formula.
/// </summary>
public class TaxCalculatorServiceTests
{
    private readonly TaxCalculatorService _sut = new();

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    private static TaxCalculationInput NewInput(
        decimal annualGross,
        string regime = "NEW",
        int month = 3,
        decimal ytdGross = 0m,
        decimal ytdTds = 0m,
        decimal pf = 0m,
        decimal pt = 0m,
        decimal investments = 0m) =>
        new(
            EmployeeId: Guid.NewGuid(),
            TaxRegime: regime,
            Month: month,
            FinancialYear: 2025,
            YearToDateGross: ytdGross,
            YearToDateTds: ytdTds,
            CurrentMonthGross: annualGross,
            PfDeduction: pf,
            ProfessionalTax: pt,
            DeclaredInvestments: investments);

    // ──────────────────────────────────────────────────────────────────────
    // NEW REGIME — Annual Tax Liability
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(300_000, 0)]       // 3L: taxable 2.25L, all in 0% band
    [InlineData(500_000, 0)]       // 5L: taxable 4.25L, slab tax 6250 but rebate wipes it
    [InlineData(775_000, 0)]       // 7.75L: taxable exactly 7L, rebate threshold hit (rebate=20k)
    public void NewRegime_AnnualTax_ZeroForIncomesWithFullRebate(decimal annualGross, decimal expectedTax)
    {
        var result = _sut.Calculate(NewInput(annualGross));
        Assert.Equal(expectedTax, result.TaxLiability);
    }

    [Fact]
    public void NewRegime_3L_TaxableIncome_Is_2_25L()
    {
        var result = _sut.Calculate(NewInput(300_000));
        Assert.Equal(225_000m, result.TaxableIncome);
    }

    [Fact]
    public void NewRegime_5L_TaxableIncome_Is_4_25L()
    {
        var result = _sut.Calculate(NewInput(500_000));
        Assert.Equal(425_000m, result.TaxableIncome);
    }

    [Fact]
    public void NewRegime_775001_ExceedsRebateThreshold_TaxPositive()
    {
        // taxable = 775,001 - 75,000 = 700,001 > 700,000 → rebate does NOT apply
        var result = _sut.Calculate(NewInput(775_001));
        Assert.True(result.TaxLiability > 0,
            $"Expected positive tax for 7.75001L gross but got {result.TaxLiability}");
    }

    [Fact]
    public void NewRegime_8L_AnnualTax_Is_23400()
    {
        // taxable = 8L - 75k = 7.25L
        // 0-3L=0, 3-7L=20000, 7-7.25L=2500; rawTax=22500
        // cess=900, total=23400
        var result = _sut.Calculate(NewInput(800_000));
        Assert.Equal(23_400m, result.TaxLiability);
    }

    [Fact]
    public void NewRegime_10L_AnnualTax_Is_44200()
    {
        // taxable = 9.25L; 0-3L=0, 3-7L=20k, 7-9.25L=22500; rawTax=42500; cess=1700
        var result = _sut.Calculate(NewInput(1_000_000));
        Assert.Equal(44_200m, result.TaxLiability);
    }

    [Fact]
    public void NewRegime_12L_AnnualTax_Is_71500()
    {
        // taxable=11.25L; 0-3=0, 3-7=20k, 7-10=30k, 10-11.25=18750; rawTax=68750; cess=2750
        var result = _sut.Calculate(NewInput(1_200_000));
        Assert.Equal(71_500m, result.TaxLiability);
    }

    [Fact]
    public void NewRegime_15L_AnnualTax_Is_130000()
    {
        // taxable=14.25L; 0-3=0, 3-7=20k, 7-10=30k, 10-12=30k, 12-14.25=45k; rawTax=125000; cess=5000
        var result = _sut.Calculate(NewInput(1_500_000));
        Assert.Equal(130_000m, result.TaxLiability);
    }

    [Fact]
    public void NewRegime_20L_AnnualTax_Is_278200()
    {
        // taxable=19.25L; ...15-19.25=127500; rawTax=267500; cess=10700
        var result = _sut.Calculate(NewInput(2_000_000));
        Assert.Equal(278_200m, result.TaxLiability);
    }

    [Fact]
    public void NewRegime_50L_NoSurcharge_AnnualTax_Is_1214200()
    {
        // taxable=49.25L < 50L (5,000,000 surcharge threshold); no surcharge
        // rawTax=1167500; cess=46700
        var result = _sut.Calculate(NewInput(5_000_000));
        Assert.Equal(1_214_200m, result.TaxLiability);
    }

    [Fact]
    public void NewRegime_55L_Surcharge10pct_Applied()
    {
        // taxable=54.25L > 50L surcharge threshold
        // rawTax=1317500; *1.10=1449250; cess=57970; total=1507220
        var result = _sut.Calculate(NewInput(5_500_000));
        Assert.Equal(1_507_220m, result.TaxLiability);
        Assert.True(result.TaxLiability > _sut.Calculate(NewInput(5_000_000)).TaxLiability,
            "55L tax must exceed 50L tax (surcharge applied)");
    }

    [Fact]
    public void NewRegime_1Cr10L_Surcharge15pct_Applied()
    {
        // taxable = 1,10,00,000 - 75,000 = 1,09,25,000 > 1,00,00,000
        // rawTax=2967500 *1.15=3412625; cess=136505; total=3549130
        var result = _sut.Calculate(NewInput(11_000_000));
        Assert.Equal(3_549_130m, result.TaxLiability);
    }

    // ──────────────────────────────────────────────────────────────────────
    // NEW REGIME — Standard deduction of 75k confirmed
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void NewRegime_StandardDeduction_75k()
    {
        var at775k = _sut.Calculate(NewInput(775_000));  // taxable=700k → rebate applies → 0
        var at776k = _sut.Calculate(NewInput(776_000));  // taxable=701k > 700k → no rebate
        Assert.Equal(0m, at775k.TaxLiability);
        Assert.True(at776k.TaxLiability > 0);
    }

    // ──────────────────────────────────────────────────────────────────────
    // OLD REGIME — Annual Tax Liability
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(300_000, 0)]   // taxable=2.5L, exactly at 0% ceiling, tax=0
    [InlineData(500_000, 0)]   // taxable=4.5L, slab tax=10k < rebate 12.5k → 0
    [InlineData(550_000, 0)]   // taxable=5L exactly, slab tax=12.5k = rebate → 0
    public void OldRegime_AnnualTax_ZeroWithRebate(decimal annualGross, decimal expectedTax)
    {
        var result = _sut.Calculate(NewInput(annualGross, "OLD"));
        Assert.Equal(expectedTax, result.TaxLiability);
    }

    [Fact]
    public void OldRegime_6L_AnnualTax_Is_23400()
    {
        // taxable=5.5L; 2.5-5L=12500, 5-5.5L=10000; rawTax=22500; cess=900
        var result = _sut.Calculate(NewInput(600_000, "OLD"));
        Assert.Equal(23_400m, result.TaxLiability);
    }

    [Fact]
    public void OldRegime_10L_AnnualTax_Is_106600()
    {
        // taxable=9.5L; 2.5-5L=12500, 5-9.5L=90000; rawTax=102500; cess=4100
        var result = _sut.Calculate(NewInput(1_000_000, "OLD"));
        Assert.Equal(106_600m, result.TaxLiability);
    }

    [Fact]
    public void OldRegime_15L_AnnualTax_Is_257400()
    {
        // taxable=14.5L; 2.5-5L=12500, 5-10L=100000, 10-14.5L=135000; rawTax=247500; cess=9900
        var result = _sut.Calculate(NewInput(1_500_000, "OLD"));
        Assert.Equal(257_400m, result.TaxLiability);
    }

    [Fact]
    public void OldRegime_20L_AnnualTax_Is_413400()
    {
        // taxable=19.5L; 2.5-5L=12500, 5-10L=100000, 10-19.5L=285000; rawTax=397500; cess=15900
        var result = _sut.Calculate(NewInput(2_000_000, "OLD"));
        Assert.Equal(413_400m, result.TaxLiability);
    }

    // ──────────────────────────────────────────────────────────────────────
    // OLD REGIME — Section 80C deduction
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void OldRegime_80C_Reduces_TaxableIncome()
    {
        var withoutDeduction = _sut.Calculate(NewInput(1_000_000, "OLD"));
        var with80C = _sut.Calculate(NewInput(1_000_000, "OLD", investments: 150_000));
        Assert.True(with80C.TaxLiability < withoutDeduction.TaxLiability,
            "80C investment of 1.5L must reduce tax");
        Assert.True(with80C.TaxableIncome < withoutDeduction.TaxableIncome);
    }

    [Fact]
    public void OldRegime_80C_Capped_At_150k()
    {
        // investments declared = 2L, but 80C cap = 1.5L → same result as 1.5L
        var at150k = _sut.Calculate(NewInput(1_000_000, "OLD", investments: 150_000));
        var at200k = _sut.Calculate(NewInput(1_000_000, "OLD", investments: 200_000));
        Assert.Equal(at150k.TaxLiability, at200k.TaxLiability);
    }

    [Fact]
    public void OldRegime_80C_With_PF_Combined()
    {
        // PF annualised = 1800/month * 12 = 21600; investments = 100000
        // Combined = 121600 < 150000 → full deduction
        var result = _sut.Calculate(NewInput(1_000_000, "OLD", pf: 1_800m, investments: 100_000));
        // taxable = 10L - 50k (std) - 1.216L (80C) = 8.284L
        Assert.True(result.TaxableIncome < 900_000m, "PT+80C must reduce taxable below 9L");
    }

    [Fact]
    public void OldRegime_80C_DoesNotApply_NewRegime()
    {
        // Same inputs; new regime ignores 80C entirely
        var old = _sut.Calculate(NewInput(1_000_000, "OLD", investments: 150_000));
        var newReg = _sut.Calculate(NewInput(1_000_000, "NEW", investments: 150_000));
        // New regime tax should be lower (lower slabs) but 80C has no effect in new regime
        var newRegNoInvestments = _sut.Calculate(NewInput(1_000_000, "NEW", investments: 0));
        Assert.Equal(newReg.TaxLiability, newRegNoInvestments.TaxLiability);
    }

    // ──────────────────────────────────────────────────────────────────────
    // NEW REGIME vs OLD REGIME comparison
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(800_000)]
    [InlineData(1_000_000)]
    [InlineData(1_200_000)]
    [InlineData(1_500_000)]
    public void NewRegime_TaxBelow12L_Lower_Than_OldRegime_WithNoDeductions(decimal annualGross)
    {
        var newTax = _sut.Calculate(NewInput(annualGross, "NEW")).TaxLiability;
        var oldTax = _sut.Calculate(NewInput(annualGross, "OLD")).TaxLiability;
        Assert.True(newTax <= oldTax,
            $"New regime tax {newTax} must be ≤ old regime tax {oldTax} for {annualGross:N0} gross");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Rebate u/s 87A — boundary conditions
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void NewRegime_Rebate87A_At_7L_Taxable_Exactly_ZeroTax()
    {
        // gross = 7,75,000 → taxable = 7,75,000 - 75,000 = 7,00,000 exactly
        var result = _sut.Calculate(NewInput(775_000));
        Assert.Equal(0m, result.TaxLiability);
        Assert.Equal(700_000m, result.TaxableIncome);
    }

    [Fact]
    public void NewRegime_Rebate87A_Just_Over_7L_Taxable_TaxApplies()
    {
        // gross = 7,75,001 → taxable = 7,00,001 → no rebate
        var result = _sut.Calculate(NewInput(775_001));
        Assert.True(result.TaxLiability > 0);
    }

    [Fact]
    public void OldRegime_Rebate87A_At_5L_Taxable_Exactly_ZeroTax()
    {
        // gross = 5,50,000 → taxable = 5,50,000 - 50,000 = 5,00,000 exactly
        var result = _sut.Calculate(NewInput(550_000, "OLD"));
        Assert.Equal(0m, result.TaxLiability);
    }

    [Fact]
    public void OldRegime_Rebate87A_Just_Over_5L_Taxable_TaxApplies()
    {
        var result = _sut.Calculate(NewInput(550_001, "OLD"));
        Assert.True(result.TaxLiability > 0);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Cess 4%
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Cess_4pct_Applied_On_Top_Of_Tax()
    {
        // 8L new regime: rawTax=22500, cess=900, total=23400
        var result = _sut.Calculate(NewInput(800_000));
        // annualTax = rawTax + cess; cess = rawTax * 0.04
        // Verify cess = annualTax - rawTax component
        // We know rawTax=22500 → cess=900 → total=23400
        Assert.Equal(23_400m, result.TaxLiability);
    }

    // ──────────────────────────────────────────────────────────────────────
    // TDS Monthly Spread
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void TDS_SpreadEvenly_In_April_OneOfTwelve()
    {
        // month=4 (April), first month of FY, uniform salary 1L/month = 12L annual
        // projectedAnnual = 0 + 1L + 1L*11 = 12L; annualTax=71500; tds=71500/12=5958
        var input = new TaxCalculationInput(
            Guid.NewGuid(), "NEW", 4, 2025,
            YearToDateGross: 0m, YearToDateTds: 0m,
            CurrentMonthGross: 100_000m,
            PfDeduction: 0m, ProfessionalTax: 0m, DeclaredInvestments: 0m);
        var result = _sut.Calculate(input);
        Assert.Equal(71_500m, result.TaxLiability);
        Assert.Equal(5_958m, result.TdsThisMonth); // round(71500/12)=5958
    }

    [Fact]
    public void TDS_Catchup_WhenYtdTdsLow_IncreasesMonthlyDeduction()
    {
        // Employee underpaid TDS for first 5 months; in October the deduction should increase
        // 12L salary, annual tax 71500; expected even = ~5958/month
        // Simulate 5 months at 0 TDS, now in month=9 (October, monthsElapsed=6, left=7)
        var input = new TaxCalculationInput(
            Guid.NewGuid(), "NEW", 9, 2025,
            YearToDateGross: 500_000m,
            YearToDateTds: 0m,            // zero TDS deducted so far
            CurrentMonthGross: 100_000m,
            PfDeduction: 0m, ProfessionalTax: 0m, DeclaredInvestments: 0m);
        var result = _sut.Calculate(input);
        // annualTax=71500; spread over 7 remaining months = ceil(71500/7)
        var expected = (long)Math.Round(71_500m / 7m, 0);
        Assert.Equal(expected, (long)result.TdsThisMonth);
        Assert.True(result.TdsThisMonth > 5_958m,
            "Catchup TDS must exceed evenly-spread TDS when prior months were zero");
    }

    [Fact]
    public void TDS_ZeroWhenYtdTdsExceedsAnnualLiability()
    {
        // Edge case: over-deducted in prior months; should return 0 not negative
        var input = new TaxCalculationInput(
            Guid.NewGuid(), "NEW", 3, 2025,
            YearToDateGross: 0m,
            YearToDateTds: 500_000m,      // impossible in reality but tests floor
            CurrentMonthGross: 800_000m,
            PfDeduction: 0m, ProfessionalTax: 0m, DeclaredInvestments: 0m);
        var result = _sut.Calculate(input);
        Assert.Equal(0m, result.TdsThisMonth);
    }

    [Fact]
    public void TDS_March_LastMonth_ClaimsFullRemainder()
    {
        // month=3 (March), monthsLeft=1 → TDS = entire remaining liability
        var input = new TaxCalculationInput(
            Guid.NewGuid(), "NEW", 3, 2025,
            YearToDateGross: 0m,
            YearToDateTds: 40_000m,         // 40k already deducted
            CurrentMonthGross: 800_000m,     // annualTax=23400
            PfDeduction: 0m, ProfessionalTax: 0m, DeclaredInvestments: 0m);
        var result = _sut.Calculate(input);
        // annualTax=23400, ytdTds=40000 → remaining = negative → clamp to 0
        Assert.Equal(0m, result.TdsThisMonth);
    }

    [Fact]
    public void TDS_March_WithPendingLiability()
    {
        // month=3, 11 months deducted 2000 each = 22000, annual=23400; pending=1400
        var input = new TaxCalculationInput(
            Guid.NewGuid(), "NEW", 3, 2025,
            YearToDateGross: 0m,
            YearToDateTds: 22_000m,
            CurrentMonthGross: 800_000m,   // annual tax=23400
            PfDeduction: 0m, ProfessionalTax: 0m, DeclaredInvestments: 0m);
        var result = _sut.Calculate(input);
        Assert.Equal(1_400m, result.TdsThisMonth);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Financial year month mapping
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(4, 12)]   // April = month 1 of FY → 11 remaining → monthsLeft=12
    [InlineData(3, 1)]    // March = month 12 of FY → 0 remaining → monthsLeft=1
    [InlineData(1, 3)]    // January = month 10 of FY → 2 remaining → monthsLeft=3
    public void FyMonthsLeft_Correct_ForKeyMonths(int calendarMonth, int expectedMonthsLeft)
    {
        // Use zero income so annualTax=0 and tds=0; verify via projection
        // Instead, verify month mapping by checking tds denominator via identical annualTax
        // Construct: YtdGross=0, CurrentMonthGross=annual so projectedAnnual=annual regardless of month
        // Then: tdsThisMonth = annualTax / monthsLeft
        // annualTax for 12L new regime = 71500; tds = 71500/monthsLeft
        decimal annualGrossPerMonth = calendarMonth >= 4
            ? 1_200_000m / 12m   // uniform
            : 1_200_000m / 12m;

        int fyMonthsElapsed = calendarMonth >= 4 ? calendarMonth - 3 : calendarMonth + 9;
        int remainingMonths = 12 - fyMonthsElapsed;
        decimal projected = annualGrossPerMonth * (remainingMonths + 1); // +1 for current

        var input = new TaxCalculationInput(
            Guid.NewGuid(), "NEW", calendarMonth, 2025,
            YearToDateGross: 0m, YearToDateTds: 0m,
            CurrentMonthGross: projected,
            PfDeduction: 0m, ProfessionalTax: 0m, DeclaredInvestments: 0m);

        var result = _sut.Calculate(input);
        if (result.TaxLiability > 0)
        {
            var impliedDenominator = Math.Round(result.TaxLiability / result.TdsThisMonth, 0);
            Assert.Equal(expectedMonthsLeft, (int)impliedDenominator);
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // RuleVersionId
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void RuleVersionId_Is_FY2024_25_V1()
    {
        var result = _sut.Calculate(NewInput(1_000_000));
        Assert.Equal("FY2024-25-V1", result.RuleVersionId);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Zero and edge inputs
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ZeroGross_ZeroTax_ZeroTds()
    {
        var result = _sut.Calculate(NewInput(0m));
        Assert.Equal(0m, result.TaxLiability);
        Assert.Equal(0m, result.TdsThisMonth);
        Assert.Equal(0m, result.TaxableIncome);
    }

    [Fact]
    public void NewRegime_TaxableIncome_NeverNegative_EvenWithLargeDeductions()
    {
        // Extreme case: PT=100k, investments=1M declared — taxable must clamp to 0
        var result = _sut.Calculate(NewInput(100_000, "OLD", pt: 100_000m, investments: 1_000_000m));
        Assert.True(result.TaxableIncome >= 0m);
    }
}
