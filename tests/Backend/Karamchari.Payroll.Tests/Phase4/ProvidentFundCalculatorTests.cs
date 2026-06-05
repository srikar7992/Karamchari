using Karamchari.Payroll.Domain.DeductionRules;
using Karamchari.Payroll.Services.Deductions;

namespace Karamchari.Payroll.Tests.Phase4;

/// <summary>
/// ProvidentFund: 12% of Basic+DA, wage ceiling ₹15,000 (statutory).
/// Voluntary PF: ceiling removed, full Basic+DA used.
/// </summary>
public class ProvidentFundCalculatorTests
{
    private readonly ProvidentFundCalculator _sut = new();

    private static DeductionContext Ctx(
        decimal basic,
        decimal da = 0m,
        bool voluntary = false,
        decimal gross = 0m) =>
        new(Guid.NewGuid(), "T1", gross == 0 ? basic + da : gross, basic, da,
            OptedVoluntaryPf: voluntary, IsEsicLocked: false, "KA", 4, 2025);

    // ──────────────────────────────────────────────────────────────────────
    // Statutory PF (ceiling ₹15,000)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void PF_BelowCeiling_12pct_Of_BasicPlusDA()
    {
        // Basic 10,000; DA 2,000; wage = 12,000 < 15,000 → PF = 12% * 12000 = 1440
        var result = _sut.Calculate(Ctx(10_000, 2_000));
        Assert.Equal(1_440m, result);
    }

    [Fact]
    public void PF_ExactlyCeiling_12pct_Of_15000()
    {
        // Basic 15,000; DA 0 → capped at 15,000 → PF = 1800
        var result = _sut.Calculate(Ctx(15_000));
        Assert.Equal(1_800m, result);
    }

    [Fact]
    public void PF_AboveCeiling_CappedAt_15000()
    {
        // Basic 25,000; statutory PF still based on ceiling 15,000 → 1800
        var result = _sut.Calculate(Ctx(25_000));
        Assert.Equal(1_800m, result);
    }

    [Fact]
    public void PF_HighSalary_StillCappedAt_1800()
    {
        // Basic 1,00,000 — PF on ceiling only
        var result = _sut.Calculate(Ctx(100_000));
        Assert.Equal(1_800m, result);
    }

    [Fact]
    public void PF_BasicPlusDA_CeilingApplied_Together()
    {
        // Basic 8,000, DA 10,000; Basic+DA = 18,000 > 15,000 → capped
        var result = _sut.Calculate(Ctx(8_000, 10_000));
        Assert.Equal(1_800m, result);
    }

    [Fact]
    public void PF_ZeroBasic_ZeroPF()
    {
        var result = _sut.Calculate(Ctx(0, 0));
        Assert.Equal(0m, result);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Voluntary PF (ceiling removed)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void VoluntaryPF_AboveCeiling_FullBasicPlusDA()
    {
        // Basic 25,000; voluntary = true → PF = 12% * 25000 = 3000
        var result = _sut.Calculate(Ctx(25_000, voluntary: true));
        Assert.Equal(3_000m, result);
    }

    [Fact]
    public void VoluntaryPF_WithDA_FullWageUsed()
    {
        // Basic 20,000, DA 5,000; voluntary → PF = 12% * 25000 = 3000
        var result = _sut.Calculate(Ctx(20_000, 5_000, voluntary: true));
        Assert.Equal(3_000m, result);
    }

    [Fact]
    public void VoluntaryPF_BelowCeiling_SameAsStatutory()
    {
        // Basic 10,000, voluntary — no ceiling to apply anyway
        var statutory = _sut.Calculate(Ctx(10_000));
        var voluntary = _sut.Calculate(Ctx(10_000, voluntary: true));
        Assert.Equal(statutory, voluntary);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Employer contribution
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void EmployerContribution_AlwaysCapped_AtCeiling()
    {
        // Even if employee opted voluntary PF, employer contribution is always capped
        // EPF: 3.67% of min(wage, 15000) + EPS: min(8.33% of min(wage,15000), 1250)
        var result = _sut.EmployerContribution(Ctx(50_000, voluntary: true));
        var atCeiling = _sut.EmployerContribution(Ctx(15_000));
        // Both should be the same since employer caps at 15k
        Assert.Equal(atCeiling, result);
    }

    [Fact]
    public void EmployerContribution_EPF_3_67pct_Plus_EPS_Capped()
    {
        // Basic 15,000; EPF = round(15000 * 0.0367) = round(550.5) = 550 (half-even: 550 is even)
        // EPS = min(round(15000 * 0.0833), 1250) = min(round(1249.5), 1250) = min(1250, 1250) = 1250
        var result = _sut.EmployerContribution(Ctx(15_000));
        Assert.Equal(550m + 1_250m, result); // 1800
    }

    [Fact]
    public void EmployerContribution_EPS_NeverExceeds_1250()
    {
        // Basic 100,000; EPS would be 8.33% * 15000 = 1250 (at ceiling)
        var result = _sut.EmployerContribution(Ctx(100_000));
        // Must stay at ceiling-based value
        Assert.Equal(_sut.EmployerContribution(Ctx(15_000)), result);
    }
}
