// -----------------------------------------------------------------------
// <copyright file="GoldenValuePayrollTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Payroll.Services.Calculation;
using Karamchari.Payroll.Services.Deductions;

namespace Karamchari.Payroll.Tests.Phase4;

/// <summary>
/// Regression suite: 16 golden-value payrolls that pin every component of net pay.
/// Any engine change that breaks a golden value is a regression.
///
/// Scenarios: low/mid/high salary, ESI eligible/boundary/ineligible/locked,
/// PF capped, voluntary PF, KA/MH PT, old/new regime, 80C, heavy OT,
/// half month, mid-year joiner, March TDS catchup.
/// </summary>
public class GoldenValuePayrollTests
{
    private static readonly Guid RunId = Guid.NewGuid();
    private static readonly Guid EmpId = Guid.NewGuid();
    private static readonly TaxCalculatorService TaxSvc = new();

    private static PayrollCalculationEngine Engine() => new([
        new ProvidentFundCalculator(),
        new EsiCalculator(),
        new ProfessionalTaxCalculator(),
    ]);

    private static StatutoryConfigSnapshot Stat() =>
        new(15_000m, 21_000m, 0.0075m, 0.0325m, 0.12m, 0.0367m, 1_250m);

    private static OvertimePolicySnapshot OtSingle(decimal mult = 1.5m) =>
        new([new OvertimeRuleSnapshot(0m, null, mult, "Regular", 1)]);

    private static List<DayAttendanceRecord> Days(
        int count, decimal hours = 8m, bool present = true,
        TimeOnly? shiftStart = null, DateOnly? startDate = null)
    {
        var d = startDate ?? new DateOnly(2025, 4, 1);
        return [..Enumerable.Range(0, count).Select(i =>
            new DayAttendanceRecord(d.AddDays(i), hours, present, false, false, shiftStart, null))];
    }

    private static EmployeePayrollInput Inp(
        decimal salary, string state = "KA", string regime = "New",
        int month = 4, int fy = 2025,
        decimal ytdGross = 0m, decimal ytdTds = 0m,
        decimal declaredInvestments = 0m,
        decimal basicPct = 40m, decimal hourlyRate = 0m,
        bool voluntaryPf = false, bool esicLocked = false,
        List<DayAttendanceRecord>? att = null,
        List<HolidayRecord>? holidays = null,
        OvertimePolicySnapshot? ot = null,
        List<ShiftPremiumSnapshot>? premiums = null,
        List<PendingAdjustmentRecord>? adj = null) =>
        new(EmpId, "Test", salary, hourlyRate, "INR", basicPct, 0m,
            att ?? Days(26),
            (IReadOnlyList<HolidayRecord>)(holidays ?? []),
            ot ?? new OvertimePolicySnapshot([]),
            (IReadOnlyList<ShiftPremiumSnapshot>)(premiums ?? []),
            (IReadOnlyList<PendingAdjustmentRecord>)(adj ?? []),
            voluntaryPf, esicLocked, state, regime,
            ytdGross, ytdTds, declaredInvestments, month, fy,
            Guid.NewGuid(), "FY2024-25-V1", Stat());

    private static decimal Net(EmployeePayrollInput i) =>
        Engine().Calculate("t1", RunId, "period", 1, "snap", i, TaxSvc).NetPay;

    private static (decimal gross, decimal pf, decimal esi, decimal pt, decimal tds, decimal net)
        All(EmployeePayrollInput i)
    {
        var r = Engine().Calculate("t1", RunId, "period", 1, "snap", i, TaxSvc);
        return (r.GrossPay, r.ProvidentFund, r.EmployeeStateInsurance,
                r.ProfessionalTax, r.TaxDeductedAtSource, r.NetPay);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Low salary band (≤ 21k): ESI applies, PT below threshold
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_15k_KA_NewRegime_BelowPTThreshold()
    {
        // basicPay=6000; PF=720; ESI=round(15000*0.0075)=round(112.5)=112; PT=0(≤15k); TDS=0
        // Net = 15000 - 720 - 112 = 14168
        var (gross, pf, esi, pt, tds, net) = All(Inp(15_000m));
        Assert.Equal(15_000m, gross);
        Assert.Equal(720m, pf);
        Assert.Equal(112m, esi);
        Assert.Equal(0m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(14_168m, net);
    }

    [Fact]
    public void Golden_21k_KA_NewRegime_ESIAtExactCeiling()
    {
        // gross=21000 ≤ 21000 ceiling → ESI applies; round(21000*0.0075)=round(157.5)=158
        // basicPay=8400; PF=1008; PT=200(≥18k); TDS=0(projected=252k, taxable=177k≤300k)
        // Net = 21000 - 1008 - 158 - 200 = 19634
        var (gross, pf, esi, pt, tds, net) = All(Inp(21_000m));
        Assert.Equal(1_008m, pf);
        Assert.Equal(158m, esi);
        Assert.Equal(200m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(19_634m, net);
    }

    [Fact]
    public void Golden_22k_KA_NewRegime_ESIJustOverCeiling()
    {
        // gross=22000 > 21000 → ESI=0; PF=round(8800*12%)=1056; PT=200; TDS=0
        // Net = 22000 - 1056 - 200 = 20744
        var (gross, pf, esi, pt, tds, net) = All(Inp(22_000m));
        Assert.Equal(0m, esi);
        Assert.Equal(1_056m, pf);
        Assert.Equal(200m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(20_744m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Mid salary (30k–70k): PF ceiling kicks in, tax rebate zone
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_45k_KA_NewRegime_PFCapped_NoTax()
    {
        // basicPay=18000>15000 → PF on ceiling: 1800; ESI=0; PT=200
        // projected=540k; taxable=465k; slab=(465k-300k)*5%=8250; rebate ≤700k → 8250; annualTax=0; TDS=0
        // Net = 45000 - 1800 - 200 = 43000
        var (gross, pf, esi, pt, tds, net) = All(Inp(45_000m));
        Assert.Equal(1_800m, pf);
        Assert.Equal(0m, esi);
        Assert.Equal(200m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(43_000m, net);
    }

    [Fact]
    public void Golden_70k_KA_NewRegime_TaxAboveRebateThreshold()
    {
        // projected=840k; taxable=840k-75k=765k > 700k → no rebate
        // slab: (700k-300k)*5%=20000 + (765k-700k)*10%=6500 = rawTax=26500
        // cess=round(26500*0.04)=1060; annualTax=27560; TDS=round(27560/12)=2297
        // Net = 70000 - 1800 - 200 - 2297 = 65703
        var (gross, pf, esi, pt, tds, net) = All(Inp(70_000m));
        Assert.Equal(1_800m, pf);
        Assert.Equal(0m, esi);
        Assert.Equal(200m, pt);
        Assert.Equal(2_297m, tds);
        Assert.Equal(65_703m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // High salary (≥ 150k): significant TDS
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_150k_KA_NewRegime_HighTax()
    {
        // projected=1800k; taxable=1725k
        // slabs: 300k→700k=20000; 700k→1000k=30000; 1000k→1200k=30000; 1200k→1500k=60000; 1500k→1725k=67500
        // rawTax=207500; cess=8300; annualTax=215800; TDS=round(215800/12)=17983
        // Net = 150000 - 1800 - 200 - 17983 = 130017
        var (_, pf, _, pt, tds, net) = All(Inp(150_000m));
        Assert.Equal(1_800m, pf);
        Assert.Equal(200m, pt);
        Assert.Equal(17_983m, tds);
        Assert.Equal(130_017m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Old regime
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_60k_KA_OldRegime_MaxInvestments()
    {
        // declared=150000; pfAnnualised=21600; 80C=min(150k+21.6k,150k)=150k
        // ptDeduction=2400; taxable=720k-50k-150k-2.4k=517.6k > 500k → no rebate
        // slab: 250k*5%=12500 + 17.6k*20%=3520 = 16020; cess=641; annualTax=16661; TDS=round(16661/12)=1388
        // Net = 60000 - 1800 - 200 - 1388 = 56612
        var (_, pf, _, pt, tds, net) = All(Inp(60_000m, regime: "Old", declaredInvestments: 150_000m));
        Assert.Equal(1_800m, pf);
        Assert.Equal(200m, pt);
        Assert.Equal(1_388m, tds);
        Assert.Equal(56_612m, net);
    }

    [Fact]
    public void Golden_60k_KA_OldRegime_NoInvestments()
    {
        // 80C=min(0+21600,150k)=21600; taxable=720k-50k-21.6k-2.4k=646k
        // slab: 250k*5%=12500 + 146k*20%=29200 = 41700; cess=1668; annualTax=43368; TDS=round(43368/12)=3614
        // Net = 60000 - 1800 - 200 - 3614 = 54386
        var (_, pf, _, pt, tds, net) = All(Inp(60_000m, regime: "Old"));
        Assert.Equal(1_800m, pf);
        Assert.Equal(200m, pt);
        Assert.Equal(3_614m, tds);
        Assert.Equal(54_386m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OT and premiums
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_30k_KA_NewRegime_HeavyOT_StillUnderTaxThreshold()
    {
        // 22 days*8h + 4 days*10h = 216 total; regular=8*26=208; OT=8hrs at 150/hr*1.5x=1800
        // gross=31800; basicPay=30k*40%=12k→PF=1440; ESI=0(>21k); PT=200
        // projected=381.6k; taxable=306.6k; slab=(306.6k-300k)*5%=330; rebate≤700k→330; TDS=0
        // Net = 31800 - 1440 - 200 = 30160
        var att = Days(22)
            .Concat(Days(4, hours: 10m, startDate: new DateOnly(2025, 4, 23)))
            .ToList();
        var r = Engine().Calculate("t1", RunId, "period", 1, "snap",
            Inp(30_000m, hourlyRate: 150m, att: att, ot: OtSingle()), TaxSvc);
        Assert.Equal(1_800m, r.OvertimePay);
        Assert.Equal(31_800m, r.GrossPay);
        Assert.Equal(1_440m, r.ProvidentFund);
        Assert.Equal(0m, r.TaxDeductedAtSource);
        Assert.Equal(30_160m, r.NetPay);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Voluntary PF
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_50k_KA_NewRegime_VoluntaryPF_NoCeiling()
    {
        // basicPay=20000; statutory PF would be 1800 (capped); voluntary removes cap → 20000*12%=2400
        // ESI=0; PT=200; taxable=525k; rebate ≤700k → TDS=0
        // Net = 50000 - 2400 - 200 = 47400
        var (_, pf, _, _, tds, net) = All(Inp(50_000m, voluntaryPf: true));
        Assert.Equal(2_400m, pf);
        Assert.Equal(0m, tds);
        Assert.Equal(47_400m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ESIC locked: contribution period closed
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_18k_KA_NewRegime_ESICLocked_NoESI()
    {
        // salary 18000 would normally attract ESI; locked=true → ESI=0
        // PF=864; PT=200; TDS=0
        // Net = 18000 - 864 - 200 = 16936 (vs 16801 when unlocked)
        var (_, pf, esi, pt, tds, net) = All(Inp(18_000m, esicLocked: true));
        Assert.Equal(864m, pf);
        Assert.Equal(0m, esi);
        Assert.Equal(200m, pt);
        Assert.Equal(16_936m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Maharashtra PT — February special (300 instead of 200)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_18k_MH_February_PTIs300()
    {
        // PT in MH: gross>10000 in February → 300 (not 200)
        // month=2; monthsElapsed=11; remaining=1; monthsLeft=2
        // projected=180000(ytd)+18000+18000*1=216000; taxable=141000≤300k; TDS=0
        // Net = 18000 - 864 - 135 - 300 = 16701
        var (_, pf, esi, pt, tds, net) = All(Inp(18_000m, state: "MH", month: 2, ytdGross: 180_000m));
        Assert.Equal(864m, pf);
        Assert.Equal(135m, esi);
        Assert.Equal(300m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(16_701m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Partial month attendance
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_30k_KA_NewRegime_HalfMonthAttendance()
    {
        // 13 present / 26 total → basePay = 30000 * 13/26 = 15000
        // basicPay = 15000*40%=6000; PF=720; ESI=112; PT=0(≤15k); TDS=0
        // Net = 15000 - 720 - 112 = 14168
        var att = Days(13)
            .Concat(Days(13, present: false, startDate: new DateOnly(2025, 4, 14)))
            .ToList();
        var r = Engine().Calculate("t1", RunId, "period", 1, "snap",
            Inp(30_000m, att: att), TaxSvc);
        Assert.Equal(15_000m, r.BasePay);
        Assert.Equal(720m, r.ProvidentFund);
        Assert.Equal(112m, r.EmployeeStateInsurance);
        Assert.Equal(0m, r.ProfessionalTax);
        Assert.Equal(14_168m, r.NetPay);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Mid-year joiner: TDS spread over fewer months
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_150k_KA_NewRegime_MidYearJoiner_October()
    {
        // Joins October (month=10); ytdGross=0; monthsElapsed=7; remaining=5; monthsLeft=6
        // projected=0+150000+150000*5=900000; taxable=825000
        // slab: (700k-300k)*5%=20000 + (825k-700k)*10%=12500 = 32500; cess=1300; annualTax=33800
        // TDS = round(33800/6) = 5633; PF=1800; PT=200
        // Net = 150000 - 1800 - 200 - 5633 = 142367
        var (_, pf, _, pt, tds, net) = All(Inp(150_000m, month: 10));
        Assert.Equal(1_800m, pf);
        Assert.Equal(200m, pt);
        Assert.Equal(5_633m, tds);
        Assert.Equal(142_367m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // March: full year TDS catchup when ytdTds=0
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_80k_KA_NewRegime_MarchCatchupTDS()
    {
        // month=3(March); monthsElapsed=12; remaining=0; monthsLeft=1
        // projected=880000(ytd)+80000+0=960000; taxable=885000
        // slab: 400k*5%=20000 + 185k*10%=18500 = 38500; cess=1540; annualTax=40040
        // TDS = round((40040-0)/1) = 40040
        // Net = 80000 - 1800 - 200 - 40040 = 37960
        var (_, pf, _, pt, tds, net) = All(Inp(80_000m, month: 3, ytdGross: 880_000m, ytdTds: 0m));
        Assert.Equal(1_800m, pf);
        Assert.Equal(200m, pt);
        Assert.Equal(40_040m, tds);
        Assert.Equal(37_960m, net);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Maharashtra ESI-eligible low salary (additional MH state coverage)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Golden_20k_MH_NewRegime_ESIEligible_NonFeb()
    {
        // state=MH, month=4; gross=20000 ≤ 21000 → ESI=round(20000*0.0075)=round(150)=150
        // basicPay=8000; PF=960; PT: MH, 20000>10000, not Feb → 200; TDS=0
        // Net = 20000 - 960 - 150 - 200 = 18690
        var (_, pf, esi, pt, tds, net) = All(Inp(20_000m, state: "MH"));
        Assert.Equal(960m, pf);
        Assert.Equal(150m, esi);
        Assert.Equal(200m, pt);
        Assert.Equal(0m, tds);
        Assert.Equal(18_690m, net);
    }
}
