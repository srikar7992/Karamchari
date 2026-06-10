// -----------------------------------------------------------------------
// <copyright file="PerStateGoldenPayrollTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Payroll.Services.Calculation;
using Karamchari.Payroll.Services.Deductions;

namespace Karamchari.Payroll.Tests.Phase4;

/// <summary>
/// One golden payroll scenario per supported PT state.
/// Pins exact net pay so any slab change fails loudly.
/// States: KA, MH already covered in GoldenValuePayrollTests.
/// This file covers: AP, TS, WB, TN, GJ, MP, OD, AS.
/// </summary>
public class PerStateGoldenPayrollTests
{
    private static readonly Guid RunId = Guid.NewGuid();
    private static readonly Guid EmpId = Guid.NewGuid();
    private static readonly TaxCalculatorService TaxSvc = new();

    private static PayrollCalculationEngine Engine() => new([
        new ProvidentFundCalculator(),
        new EsiCalculator(),
        new ProfessionalTaxCalculator(),
    ]);

    private static List<DayAttendanceRecord> FullMonth() =>
        [..Enumerable.Range(0, 26).Select(i =>
            new DayAttendanceRecord(
                new DateOnly(2025, 4, 1).AddDays(i), 8m, true, false, false, null, null))];

    private static EmployeePayrollInput Inp(decimal salary, string state) =>
        new(EmpId, "Test", salary, 0m, "INR", 40m, 0m,
            FullMonth(),
            Array.Empty<HolidayRecord>(),
            new OvertimePolicySnapshot([]),
            Array.Empty<ShiftPremiumSnapshot>(),
            Array.Empty<PendingAdjustmentRecord>(),
            false, false, state, "New",
            0m, 0m, 0m, 4, 2025,
            Guid.NewGuid(), "FY2024-25-V1",
            new StatutoryConfigSnapshot(15_000m, 21_000m, 0.0075m, 0.0325m, 0.12m, 0.0367m, 1_250m));

    private static (decimal pf, decimal esi, decimal pt, decimal tds, decimal net) All(decimal salary, string state)
    {
        var r = Engine().Calculate("t1", RunId, "Apr 2025", 1, "snap", Inp(salary, state), TaxSvc);
        return (r.ProvidentFund, r.EmployeeStateInsurance, r.ProfessionalTax,
                r.TaxDeductedAtSource, r.NetPay);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Andhra Pradesh: ≤15k=0, 15001-20000=150, >20000=200
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_AP_18k_PtMidBracket()
    {
        // AP: 15001-20000 → PT=150; basicPay=7200; PF=864; ESI=135; TDS=0
        // Net = 18000 - 864 - 135 - 150 = 16851
        var (pf, esi, pt, tds, net) = All(18_000m, "AP");
        Assert.Equal(864m, pf);
        Assert.Equal(135m, esi);
        Assert.Equal(150m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(16_851m, net);
    }

    [Fact]
    public void Golden_AP_25k_PtTopBracket()
    {
        // AP: >20000 → PT=200; ESI=0 (>21k); PF=1200; TDS=0
        // Net = 25000 - 1200 - 0 - 200 = 23600
        var (pf, esi, pt, tds, net) = All(25_000m, "AP");
        Assert.Equal(1_200m, pf);
        Assert.Equal(0m, esi);
        Assert.Equal(200m, pt);
        Assert.Equal(23_600m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Telangana: same slabs as AP
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_TS_18k_SameAsAP()
    {
        // TS delegates to AP slabs: 15001-20000 → PT=150
        // Net = 18000 - 864 - 135 - 150 = 16851
        var (pf, esi, pt, _, net) = All(18_000m, "TS");
        Assert.Equal(864m, pf);
        Assert.Equal(135m, esi);
        Assert.Equal(150m, pt);
        Assert.Equal(16_851m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // West Bengal: ≤10k=0, 10001-15000=110, 15001-25000=130, 25001-40000=150, >40000=200
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_WB_20k_PtMidBracket()
    {
        // WB: 15001-25000 → PT=130; ESI: 20000≤21000 → 150; PF=960; TDS=0
        // Net = 20000 - 960 - 150 - 130 = 18760
        var (pf, esi, pt, tds, net) = All(20_000m, "WB");
        Assert.Equal(960m, pf);
        Assert.Equal(150m, esi);
        Assert.Equal(130m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(18_760m, net);
    }

    [Fact]
    public void Golden_WB_12k_LowBracket()
    {
        // WB: 10001-15000 → PT=110; ESI: 12000≤21000 → round(90)=90; PF=576; TDS=0
        // Net = 12000 - 576 - 90 - 110 = 11224
        var (pf, esi, pt, tds, net) = All(12_000m, "WB");
        Assert.Equal(576m, pf);
        Assert.Equal(90m, esi);
        Assert.Equal(110m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(11_224m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tamil Nadu: ≤21k=0, >21k=208
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_TN_25k_PtApplies()
    {
        // TN: 25000>21000 → PT=208; ESI=0 (>21k); PF=1200; TDS=0
        // Net = 25000 - 1200 - 0 - 208 = 23592
        var (pf, esi, pt, tds, net) = All(25_000m, "TN");
        Assert.Equal(1_200m, pf);
        Assert.Equal(0m, esi);
        Assert.Equal(208m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(23_592m, net);
    }

    [Fact]
    public void Golden_TN_20k_PtZero()
    {
        // TN: 20000≤21000 → PT=0; ESI: 150; PF=960; TDS=0
        // Net = 20000 - 960 - 150 - 0 = 18890
        var (pf, esi, pt, tds, net) = All(20_000m, "TN");
        Assert.Equal(0m, pt);
        Assert.Equal(18_890m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Gujarat: ≤5999=0, 6000-8999=80, 9000-11999=150, ≥12000=200
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_GJ_15k_PtTopBracket()
    {
        // GJ: 15000≥12000 → PT=200; ESI: 15000→112; PF=720; TDS=0
        // Net = 15000 - 720 - 112 - 200 = 13968
        var (pf, esi, pt, tds, net) = All(15_000m, "GJ");
        Assert.Equal(720m, pf);
        Assert.Equal(112m, esi);
        Assert.Equal(200m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(13_968m, net);
    }

    [Fact]
    public void Golden_GJ_10k_PtMidBracket()
    {
        // GJ: 9000-11999 → PT=150; ESI: 10000→75; PF=480; TDS=0
        // Net = 10000 - 480 - 75 - 150 = 9295
        var (pf, esi, pt, tds, net) = All(10_000m, "GJ");
        Assert.Equal(480m, pf);
        Assert.Equal(75m, esi);
        Assert.Equal(150m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(9_295m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Madhya Pradesh: ≤18750=0, >18750=208
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_MP_20k_PtApplies()
    {
        // MP: 20000>18750 → PT=208; ESI: 20000→150; PF=960; TDS=0
        // Net = 20000 - 960 - 150 - 208 = 18682
        var (pf, esi, pt, tds, net) = All(20_000m, "MP");
        Assert.Equal(960m, pf);
        Assert.Equal(150m, esi);
        Assert.Equal(208m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(18_682m, net);
    }

    [Fact]
    public void Golden_MP_18k_PtZero()
    {
        // MP: 18000≤18750 → PT=0; ESI: 18000→135; PF=864; TDS=0
        // Net = 18000 - 864 - 135 - 0 = 17001
        var (pf, esi, pt, tds, net) = All(18_000m, "MP");
        Assert.Equal(864m, pf);
        Assert.Equal(135m, esi);
        Assert.Equal(0m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(17_001m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Odisha: ≤13333=0, 13334-25000=125, >25000=200
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_OD_20k_PtMidBracket()
    {
        // OD: 13334-25000 → PT=125; ESI: 150; PF=960; TDS=0
        // Net = 20000 - 960 - 150 - 125 = 18765
        var (pf, esi, pt, tds, net) = All(20_000m, "OD");
        Assert.Equal(960m, pf);
        Assert.Equal(150m, esi);
        Assert.Equal(125m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(18_765m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Assam: ≤10000=0, >10000=208
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_AS_15k_PtApplies()
    {
        // AS: 15000>10000 → PT=208; ESI: 112; PF=720; TDS=0
        // Net = 15000 - 720 - 112 - 208 = 13960
        var (pf, esi, pt, tds, net) = All(15_000m, "AS");
        Assert.Equal(720m, pf);
        Assert.Equal(112m, esi);
        Assert.Equal(208m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(13_960m, net);
    }

    [Fact]
    public void Golden_AS_10k_PtZero()
    {
        // AS: 10000≤10000 → PT=0; ESI: 75; PF=480; TDS=0
        // Net = 10000 - 480 - 75 - 0 = 9445
        var (pf, esi, pt, tds, net) = All(10_000m, "AS");
        Assert.Equal(480m, pf);
        Assert.Equal(75m, esi);
        Assert.Equal(0m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(9_445m, net);
    }
}
