// -----------------------------------------------------------------------
// <copyright file="PayrollCalculationEngineTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Payroll.Domain.Adjustments;
using Karamchari.Payroll.Domain.Results;
using Karamchari.Payroll.Services.Calculation;
using Karamchari.Payroll.Services.Deductions;

namespace Karamchari.Payroll.Tests.Phase4;

/// <summary>
/// Integration tests for PayrollCalculationEngine.
/// Pipeline order: attendance → base pay → OT → holiday → premiums → deductions → tax → net.
/// Tests prove: correct component values, ordering invariants, deduction/tax integration.
/// </summary>
public class PayrollCalculationEngineTests
{
    private static readonly Guid RunId = Guid.NewGuid();
    private static readonly Guid EmpId = Guid.NewGuid();
    private static readonly TaxCalculatorService TaxSvc = new();

    private static PayrollCalculationEngine Engine() => new([
        new ProvidentFundCalculator(),
        new EsiCalculator(),
        new ProfessionalTaxCalculator(),
    ]);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<DayAttendanceRecord> Days(
        int count,
        decimal hours = 8m,
        bool present = true,
        bool isHoliday = false,
        TimeOnly? shiftStart = null,
        DateOnly? startDate = null)
    {
        var d = startDate ?? new DateOnly(2025, 4, 1);
        return [..Enumerable.Range(0, count).Select(i =>
            new DayAttendanceRecord(d.AddDays(i), hours, present, false, isHoliday, shiftStart, null))];
    }

    private static EmployeePayrollInput Inp(
        decimal salary = 30_000m,
        decimal hourlyRate = 0m,
        string state = "KA",
        string regime = "New",
        int month = 4,
        decimal ytdGross = 0m,
        decimal ytdTds = 0m,
        decimal declaredInvestments = 0m,
        List<DayAttendanceRecord>? att = null,
        List<HolidayRecord>? holidays = null,
        OvertimePolicySnapshot? ot = null,
        List<ShiftPremiumSnapshot>? premiums = null,
        List<PendingAdjustmentRecord>? adj = null,
        decimal basicPct = 40m,
        bool esicLocked = false,
        bool voluntaryPf = false) =>
        new(EmpId, "Test Employee", salary, hourlyRate, "INR", basicPct, 0m,
            att ?? Days(26),
            (IReadOnlyList<HolidayRecord>)(holidays ?? []),
            ot ?? new OvertimePolicySnapshot([]),
            (IReadOnlyList<ShiftPremiumSnapshot>)(premiums ?? []),
            (IReadOnlyList<PendingAdjustmentRecord>)(adj ?? []),
            voluntaryPf, esicLocked, state, regime,
            ytdGross, ytdTds, declaredInvestments, month, 2025,
            Guid.NewGuid(), "FY2024-25-V1",
            new StatutoryConfigSnapshot(15_000m, 21_000m, 0.0075m, 0.0325m, 0.12m, 0.0367m, 1_250m));

    private static EmployeePayrollResult Calc(EmployeePayrollInput input) =>
        Engine().Calculate("t1", RunId, "Apr 2025", 1, "snap1", input, TaxSvc);

    private static OvertimePolicySnapshot OtPolicy(
        params (decimal from, decimal? to, decimal mult, int pri)[] rules) =>
        new([..rules.Select(r =>
            new OvertimeRuleSnapshot(r.from, r.to, r.mult, "Regular", r.pri))]);

    private static ShiftPremiumSnapshot NightPremium(decimal pct) =>
        new("Night", pct, new TimeOnly(22, 0), new TimeOnly(6, 0), false, false);

    private static ShiftPremiumSnapshot WeekendPremium(decimal pct) =>
        new("Weekend", pct, null, null, true, false);

    private static ShiftPremiumSnapshot HolidayPremium(decimal pct) =>
        new("Holiday", pct, null, null, false, true);

    // ── Base pay / proration ──────────────────────────────────────────────────

    [Fact]
    public void BasePay_FullAttendance_EqualsSalary()
    {
        var r = Calc(Inp(salary: 30_000m));
        Assert.Equal(30_000m, r.BasePay);
        Assert.Equal(30_000m, r.GrossPay);
    }

    [Fact]
    public void BasePay_HalfAttendance_HalfSalary()
    {
        var att = Days(13)
            .Concat(Days(13, present: false, startDate: new DateOnly(2025, 4, 14)))
            .ToList();
        // 13 present / 26 working = 50%; 26000 * 0.5 = 13000
        var r = Calc(Inp(salary: 26_000m, att: att));
        Assert.Equal(13_000m, r.BasePay);
    }

    [Fact]
    public void BasePay_OneAbsentDay_MinusOneDailyRate()
    {
        var att = Days(25)
            .Concat(Days(1, present: false, startDate: new DateOnly(2025, 4, 26)))
            .ToList();
        // 26000 * 25/26 = 25000
        var r = Calc(Inp(salary: 26_000m, att: att));
        Assert.Equal(25_000m, r.BasePay);
    }

    [Fact]
    public void BasePay_ZeroPresent_Zero()
    {
        var att = Days(26, present: false);
        var r = Calc(Inp(salary: 30_000m, att: att));
        Assert.Equal(0m, r.BasePay);
        Assert.Equal(0m, r.GrossPay);
    }

    [Fact]
    public void BasePay_SingleDayMonth_FullSalary()
    {
        var att = Days(1);
        var r = Calc(Inp(salary: 30_000m, att: att));
        // 1/1 = full salary
        Assert.Equal(30_000m, r.BasePay);
    }

    // ── Overtime ──────────────────────────────────────────────────────────────

    [Fact]
    public void OT_ExactlyEightHoursPerDay_NoOT()
    {
        var r = Calc(Inp(att: Days(26, hours: 8m), ot: OtPolicy((0, null, 1.5m, 1))));
        Assert.Equal(0m, r.OvertimePay);
    }

    [Fact]
    public void OT_NoPolicyRules_ZeroOT()
    {
        var r = Calc(Inp(att: Days(26, hours: 10m), ot: new OvertimePolicySnapshot([])));
        Assert.Equal(0m, r.OvertimePay);
    }

    [Fact]
    public void OT_Tier1_FourHoursAt1_5x()
    {
        // 24 days at 8h + 2 days at 10h → 212 total; regular = 208; OT = 4
        var att = Days(24)
            .Concat(Days(2, hours: 10m, startDate: new DateOnly(2025, 4, 25)))
            .ToList();
        var r = Calc(Inp(salary: 30_000m, hourlyRate: 150m, att: att,
            ot: OtPolicy((0, null, 1.5m, 1))));
        // 150 * 1.5 * 4 = 900
        Assert.Equal(900m, r.OvertimePay);
    }

    [Fact]
    public void OT_Tier1Boundary_ExactAtTierCap()
    {
        // 24 days at 8h + 2 days at 12h → OT = 8; tier1 cap = 4 → spills to tier2
        var att = Days(24)
            .Concat(Days(2, hours: 12m, startDate: new DateOnly(2025, 4, 25)))
            .ToList();
        // tier1: 0-4 @ 1.5x; tier2: 4+ @ 2x
        var r = Calc(Inp(hourlyRate: 100m, att: att,
            ot: OtPolicy((0, 4m, 1.5m, 1), (4m, null, 2.0m, 2))));
        // tier1: 4 * 100 * 1.5 = 600; tier2: 4 * 100 * 2 = 800; total = 1400
        Assert.Equal(1_400m, r.OvertimePay);
    }

    [Fact]
    public void OT_TwoTiers_Correct()
    {
        // 16 days at 8h + 10 days at 10h → total 228; regular 208; OT 20
        var att = Days(16)
            .Concat(Days(10, hours: 10m, startDate: new DateOnly(2025, 4, 17)))
            .ToList();
        // tier1: 0-10 @ 1.5x; tier2: 10+ @ 2x
        var r = Calc(Inp(salary: 30_000m, hourlyRate: 150m, att: att,
            ot: OtPolicy((0, 10m, 1.5m, 1), (10m, null, 2.0m, 2))));
        // tier1: 10 * 150 * 1.5 = 2250; tier2: 10 * 150 * 2.0 = 3000; total = 5250
        Assert.Equal(5_250m, r.OvertimePay);
    }

    [Fact]
    public void OT_HourlyRateFromInput_WhenSet()
    {
        // All 26 days at 9h → 26 OT hrs
        var r = Calc(Inp(salary: 30_000m, hourlyRate: 200m,
            att: Days(26, hours: 9m), ot: OtPolicy((0, null, 1.5m, 1))));
        // 200 * 1.5 * 26 = 7800
        Assert.Equal(7_800m, r.OvertimePay);
    }

    [Fact]
    public void OT_HourlyRateDerived_WhenZero()
    {
        // salary = 20800; 26 days; derived rate = 20800/(26*8) = 100
        var r = Calc(Inp(salary: 20_800m, hourlyRate: 0m,
            att: Days(26, hours: 9m), ot: OtPolicy((0, null, 1.5m, 1))));
        // derived rate=100; OT=26 hrs; 100 * 1.5 * 26 = 3900
        Assert.Equal(3_900m, r.OvertimePay);
    }

    [Fact]
    public void OT_IncludedInGrossPay()
    {
        var att = Days(24)
            .Concat(Days(2, hours: 10m, startDate: new DateOnly(2025, 4, 25)))
            .ToList();
        var r = Calc(Inp(salary: 30_000m, hourlyRate: 150m, att: att,
            ot: OtPolicy((0, null, 1.5m, 1))));
        Assert.Equal(r.BasePay + r.OvertimePay + r.HolidayPay + r.ShiftPremium, r.GrossPay);
    }

    // ── Holiday pay ───────────────────────────────────────────────────────────

    [Fact]
    public void HolidayPay_TwoDaysWorked_DailyRate()
    {
        var att = Days(26)
            .Concat([
                new DayAttendanceRecord(new DateOnly(2025, 4, 27), 8m, true, false, true, null, null),
                new DayAttendanceRecord(new DateOnly(2025, 4, 28), 8m, true, false, true, null, null),
            ])
            .ToList();
        // dailyRate = 26000/26 = 1000; 2 * 1000 = 2000
        var r = Calc(Inp(salary: 26_000m, att: att));
        Assert.Equal(2_000m, r.HolidayPay);
        Assert.Equal(28_000m, r.GrossPay);
    }

    [Fact]
    public void HolidayPay_AbsentOnHoliday_Zero()
    {
        var att = Days(26)
            .Concat([new DayAttendanceRecord(new DateOnly(2025, 4, 27), 0m, false, false, true, null, null)])
            .ToList();
        var r = Calc(Inp(salary: 26_000m, att: att));
        Assert.Equal(0m, r.HolidayPay);
    }

    [Fact]
    public void HolidayPay_UsesWorkingDaysAsDenominator()
    {
        // 13 working days + 1 holiday; dailyRate = 13000/13 = 1000
        var att = Days(13)
            .Concat([new DayAttendanceRecord(new DateOnly(2025, 4, 14), 8m, true, false, true, null, null)])
            .ToList();
        var r = Calc(Inp(salary: 13_000m, att: att));
        Assert.Equal(1_000m, r.HolidayPay);
    }

    // ── Shift premiums ────────────────────────────────────────────────────────

    [Fact]
    public void Premium_NightShift_AllDays()
    {
        // 26 days at 22:00 shift start; night window 22:00-06:00; 30%
        var att = Days(26, shiftStart: new TimeOnly(22, 0));
        var r = Calc(Inp(salary: 26_000m, att: att, premiums: [NightPremium(30m)]));
        // dailyBase = 26000/26 = 1000; 26 * 1000 * 30% = 7800
        Assert.Equal(7_800m, r.ShiftPremium);
    }

    [Fact]
    public void Premium_NightShift_HalfDays()
    {
        var att = Days(13, shiftStart: new TimeOnly(22, 0))
            .Concat(Days(13, shiftStart: new TimeOnly(9, 0), startDate: new DateOnly(2025, 4, 14)))
            .ToList();
        var r = Calc(Inp(salary: 26_000m, att: att, premiums: [NightPremium(30m)]));
        // 13 nights * 1000 * 30% = 3900
        Assert.Equal(3_900m, r.ShiftPremium);
    }

    [Fact]
    public void Premium_DayShift_NoPremium()
    {
        var att = Days(26, shiftStart: new TimeOnly(9, 0));
        var r = Calc(Inp(salary: 26_000m, att: att, premiums: [NightPremium(30m)]));
        Assert.Equal(0m, r.ShiftPremium);
    }

    [Fact]
    public void Premium_NightWindow_Wraps_Midnight()
    {
        // Shift at 23:00; window 22:00-06:00 wraps midnight → applies
        var att = Days(5, shiftStart: new TimeOnly(23, 0));
        var r = Calc(Inp(salary: 26_000m, att: att, premiums: [NightPremium(20m)]));
        // dailyBase = 26000/5 = 5200; 5 * 5200 * 20% = 5200
        Assert.Equal(5_200m, r.ShiftPremium);
    }

    [Fact]
    public void Premium_Weekend_SevenDaysInApril()
    {
        // April 1-26 2025: Sat/Sun on 5,6,12,13,19,20,26 = 7 weekend days
        var att = Days(26);
        var r = Calc(Inp(salary: 26_000m, att: att, premiums: [WeekendPremium(20m)]));
        // dailyBase = 26000/26 = 1000; 7 * 1000 * 20% = 1400
        Assert.Equal(1_400m, r.ShiftPremium);
    }

    [Fact]
    public void Premium_Holiday_TwoDaysInHolidayList()
    {
        var att = Days(26);
        var holidays = new List<HolidayRecord>
        {
            new(new DateOnly(2025, 4, 10), "Ambedkar Jayanti"),
            new(new DateOnly(2025, 4, 14), "Vishu"),
        };
        var r = Calc(Inp(salary: 26_000m, att: att,
            holidays: holidays, premiums: [HolidayPremium(50m)]));
        // 2 * 1000 * 50% = 1000
        Assert.Equal(1_000m, r.ShiftPremium);
    }

    [Fact]
    public void Premium_Overlapping_Night_And_Weekend_Accumulated()
    {
        // April 5 = Saturday; shift at 22:00 → both premiums apply
        var att = new List<DayAttendanceRecord>
        {
            new(new DateOnly(2025, 4, 5), 8m, true, false, false, new TimeOnly(22, 0), null),
        };
        var r = Calc(Inp(salary: 26_000m, att: att,
            premiums: [NightPremium(30m), WeekendPremium(20m)]));
        // 1 present day; dailyBase = 26000/1 = 26000
        // night: 26000 * 30% = 7800; weekend: 26000 * 20% = 5200; total = 13000
        Assert.Equal(13_000m, r.ShiftPremium);
    }

    [Fact]
    public void Premium_NoPremiumRules_Zero()
    {
        var att = Days(26, shiftStart: new TimeOnly(22, 0));
        var r = Calc(Inp(salary: 30_000m, att: att, premiums: []));
        Assert.Equal(0m, r.ShiftPremium);
    }

    // ── Deduction integration ─────────────────────────────────────────────────

    [Fact]
    public void PF_CalculatedOnBasicPay_NotGross()
    {
        // OT adds to gross but PF still uses basicPay from basePay
        var att = Days(24).Concat(Days(2, hours: 10m, startDate: new DateOnly(2025, 4, 25))).ToList();
        var withOt = Calc(Inp(salary: 30_000m, hourlyRate: 150m, att: att,
            ot: OtPolicy((0, null, 1.5m, 1))));
        var withoutOt = Calc(Inp(salary: 30_000m));
        // PF same since basePay = 30000 in both cases
        Assert.Equal(withoutOt.ProvidentFund, withOt.ProvidentFund);
    }

    [Fact]
    public void PF_BelowCeiling_12pctOfBasicPay()
    {
        // basicPay = 30000 * 40% = 12000 < 15000; PF = 1440
        var r = Calc(Inp(salary: 30_000m));
        Assert.Equal(1_440m, r.ProvidentFund);
    }

    [Fact]
    public void PF_AboveCeiling_CappedAt1800()
    {
        // basicPay = 80000 * 40% = 32000 > 15000; PF capped = 1800
        var r = Calc(Inp(salary: 80_000m));
        Assert.Equal(1_800m, r.ProvidentFund);
    }

    [Fact]
    public void PF_VoluntaryPF_RemovesCeiling()
    {
        // basicPay = 40000 * 40% = 16000 > 15000; voluntary → 12% * 16000 = 1920
        var r = Calc(Inp(salary: 40_000m, voluntaryPf: true));
        Assert.Equal(1_920m, r.ProvidentFund);
    }

    [Fact]
    public void ESI_BelowThreshold_Applies()
    {
        // gross = 18000 ≤ 21000; 0.75% * 18000 = 135
        var r = Calc(Inp(salary: 18_000m));
        Assert.Equal(135m, r.EmployeeStateInsurance);
    }

    [Fact]
    public void ESI_AboveThreshold_Zero()
    {
        var r = Calc(Inp(salary: 30_000m));
        Assert.Equal(0m, r.EmployeeStateInsurance);
    }

    [Fact]
    public void ESI_Locked_Zero()
    {
        var r = Calc(Inp(salary: 18_000m, esicLocked: true));
        Assert.Equal(0m, r.EmployeeStateInsurance);
    }

    [Fact]
    public void ESI_OTCrossesThreshold_ESIBecomesZero()
    {
        // Base gross = 18000; OT adds 6000; gross = 24000 > 21000 → ESI = 0
        var att = Days(24).Concat(Days(2, hours: 10m, startDate: new DateOnly(2025, 4, 25))).ToList();
        var r = Calc(Inp(salary: 18_000m, hourlyRate: 1_000m, att: att,
            ot: OtPolicy((0, null, 1.5m, 1))));
        Assert.True(r.GrossPay > 21_000m);
        Assert.Equal(0m, r.EmployeeStateInsurance);
    }

    [Fact]
    public void PT_Karnataka_Above18k()
    {
        var r = Calc(Inp(salary: 30_000m, state: "KA"));
        Assert.Equal(200m, r.ProfessionalTax);
    }

    [Fact]
    public void PT_Maharashtra_February_300()
    {
        var r = Calc(Inp(salary: 30_000m, state: "MH", month: 2));
        Assert.Equal(300m, r.ProfessionalTax);
    }

    [Fact]
    public void PT_Karnataka_Below15k_Zero()
    {
        // gross = 14000 ≤ 15000 → KA PT = 0
        var att = Days(13).Concat(Days(13, present: false, startDate: new DateOnly(2025, 4, 14))).ToList();
        var r = Calc(Inp(salary: 28_000m, att: att, state: "KA"));
        // basePay = 28000 * 13/26 = 14000
        Assert.Equal(14_000m, r.BasePay);
        Assert.Equal(0m, r.ProfessionalTax);
    }

    // ── Tax integration ───────────────────────────────────────────────────────

    [Fact]
    public void Tax_NewRegime_BelowSlab_ZeroTDS()
    {
        // annual = 30000 * 12 = 360k; taxable = 285k < 300k → 0
        var r = Calc(Inp(salary: 30_000m));
        Assert.Equal(0m, r.TaxDeductedAtSource);
    }

    [Fact]
    public void Tax_NewRegime_AboveSlab_TDSDeducted()
    {
        // annual = 80000 * 12 = 960k; taxable = 885k; TDS = 3337
        var r = Calc(Inp(salary: 80_000m));
        Assert.Equal(3_337m, r.TaxDeductedAtSource);
    }

    [Fact]
    public void Tax_OldRegime_80CReducesTaxableIncome()
    {
        // Old regime, 80k salary; PF 1800/mo → 80C = 21600; PT 200/mo → deduction
        // taxable = 960000 - 50000 - 21600 - 2400 = 886000
        // tax on 886k old regime: 12500+77200=89700; cess=3588; annual=93288; TDS=7774
        var r = Calc(Inp(salary: 80_000m, regime: "Old"));
        Assert.Equal(7_774m, r.TaxDeductedAtSource);
    }

    [Fact]
    public void Tax_OT_IncreasesGross_KicksInTDS()
    {
        // salary 30k alone → below tax threshold; with OT → TDS kicks in
        // HourlyRate=1000, 26 days at 9h → 26 OT hrs; gross = 30000+39000 = 69000
        // annual = 69000*12 = 828000; taxable = 753000; TDS = 2193
        var r = Calc(Inp(salary: 30_000m, hourlyRate: 1_000m,
            att: Days(26, hours: 9m), ot: OtPolicy((0, null, 1.5m, 1))));
        Assert.Equal(2_193m, r.TaxDeductedAtSource);
    }

    [Fact]
    public void Tax_BasedOnGrossPayNotAdjustedGross()
    {
        // Adjustments don't affect tax base
        var base_ = Calc(Inp(salary: 80_000m));
        var withCredit = Calc(Inp(salary: 80_000m,
            adj: [new(Guid.NewGuid(), 10_000m, "Credit", "Bonus")]));
        Assert.Equal(base_.TaxDeductedAtSource, withCredit.TaxDeductedAtSource);
    }

    [Fact]
    public void Tax_YtdTDS_ReducesCurrentMonth()
    {
        var r1 = Calc(Inp(salary: 80_000m, ytdTds: 0m));
        var r2 = Calc(Inp(salary: 80_000m, ytdTds: 30_000m));
        Assert.True(r2.TaxDeductedAtSource < r1.TaxDeductedAtSource);
    }

    [Fact]
    public void Tax_OverDeducted_ZeroTDS()
    {
        // Over-withheld → TDS = max(0, ...)
        var r = Calc(Inp(salary: 30_000m, ytdTds: 100_000m));
        Assert.Equal(0m, r.TaxDeductedAtSource);
    }

    [Fact]
    public void Tax_MarchLastMonth_CatchupWhenYtdTdsZero()
    {
        // March with 11 months of YTD gross accumulated; full annual tax must be deducted now
        // ytdGross = 80000 * 11 = 880000; current month = 80000; projected = 880000 + 80000 = 960000
        // annual tax = 40040; YtdTds = 0 → all 40040 falls in March (monthsLeft=1)
        var r = Calc(Inp(salary: 80_000m, month: 3, ytdGross: 80_000m * 11m, ytdTds: 0m));
        Assert.Equal(40_040m, r.TaxDeductedAtSource);
    }

    // ── Adjustments are post-tax ──────────────────────────────────────────────

    [Fact]
    public void Adjustment_Credit_AddsToNetPay()
    {
        var base_ = Calc(Inp(salary: 30_000m));
        var r = Calc(Inp(salary: 30_000m,
            adj: [new(Guid.NewGuid(), 2_000m, "Credit", "Bonus")]));
        Assert.Equal(base_.NetPay + 2_000m, r.NetPay);
    }

    [Fact]
    public void Adjustment_Debit_ReducesNetPay()
    {
        var base_ = Calc(Inp(salary: 30_000m));
        var r = Calc(Inp(salary: 30_000m,
            adj: [new(Guid.NewGuid(), 1_500m, "Debit", "Loan")]));
        Assert.Equal(base_.NetPay - 1_500m, r.NetPay);
    }

    [Fact]
    public void Adjustment_Credit_DoesNotAffectTax()
    {
        var base_ = Calc(Inp(salary: 80_000m));
        var r = Calc(Inp(salary: 80_000m,
            adj: [new(Guid.NewGuid(), 10_000m, "Credit", "Bonus")]));
        Assert.Equal(base_.TaxDeductedAtSource, r.TaxDeductedAtSource);
    }

    [Fact]
    public void Adjustment_Debit_DoesNotAffectTax()
    {
        var base_ = Calc(Inp(salary: 80_000m));
        var r = Calc(Inp(salary: 80_000m,
            adj: [new(Guid.NewGuid(), 5_000m, "Debit", "Loan")]));
        Assert.Equal(base_.TaxDeductedAtSource, r.TaxDeductedAtSource);
    }

    [Fact]
    public void Adjustment_MultipleCreditsAndDebits_NetEffect()
    {
        var base_ = Calc(Inp(salary: 30_000m));
        // net = +3000 +1000 -2000 = +2000
        var r = Calc(Inp(salary: 30_000m, adj: [
            new(Guid.NewGuid(), 3_000m, "Credit", "Bonus"),
            new(Guid.NewGuid(), 1_000m, "Credit", "Medical"),
            new(Guid.NewGuid(), 2_000m, "Debit", "Loan"),
        ]));
        Assert.Equal(base_.NetPay + 2_000m, r.NetPay);
    }

    // ── Net pay composition ───────────────────────────────────────────────────

    [Fact]
    public void NetPay_EqualsGrossMinusDeductions()
    {
        var r = Calc(Inp(salary: 30_000m));
        Assert.Equal(r.GrossPay - r.TotalDeductions, r.NetPay);
    }

    [Fact]
    public void GrossPay_SumOfEarningComponents()
    {
        var r = Calc(Inp(salary: 26_000m, hourlyRate: 100m,
            att: Days(24).Concat(Days(2, hours: 10m, startDate: new DateOnly(2025, 4, 25))).ToList(),
            ot: OtPolicy((0, null, 1.5m, 1)),
            premiums: [NightPremium(20m)]));
        Assert.Equal(r.BasePay + r.OvertimePay + r.HolidayPay + r.ShiftPremium, r.GrossPay);
    }

    [Fact]
    public void TotalDeductions_SumOfComponents()
    {
        var r = Calc(Inp(salary: 18_000m, state: "MH"));
        Assert.Equal(
            r.ProvidentFund + r.EmployeeStateInsurance + r.ProfessionalTax + r.TaxDeductedAtSource,
            r.TotalDeductions);
    }

    // ── End-to-end golden values ──────────────────────────────────────────────

    [Fact]
    public void E2E_30k_KA_NewRegime_FullMonth()
    {
        // PF=1440, ESI=0, PT=200, TDS=0; net=28360
        var r = Calc(Inp(salary: 30_000m));
        Assert.Equal(30_000m, r.BasePay);
        Assert.Equal(1_440m, r.ProvidentFund);
        Assert.Equal(0m, r.EmployeeStateInsurance);
        Assert.Equal(200m, r.ProfessionalTax);
        Assert.Equal(0m, r.TaxDeductedAtSource);
        Assert.Equal(28_360m, r.NetPay);
    }

    [Fact]
    public void E2E_80k_KA_NewRegime_FullMonth()
    {
        // PF=1800, ESI=0, PT=200, TDS=3337; net=74663
        var r = Calc(Inp(salary: 80_000m));
        Assert.Equal(80_000m, r.BasePay);
        Assert.Equal(1_800m, r.ProvidentFund);
        Assert.Equal(0m, r.EmployeeStateInsurance);
        Assert.Equal(200m, r.ProfessionalTax);
        Assert.Equal(3_337m, r.TaxDeductedAtSource);
        Assert.Equal(74_663m, r.NetPay);
    }

    [Fact]
    public void E2E_18k_KA_NewRegime_ESIApplies()
    {
        // PF=864, ESI=135, PT=200, TDS=0; net=16801
        var r = Calc(Inp(salary: 18_000m));
        Assert.Equal(864m, r.ProvidentFund);
        Assert.Equal(135m, r.EmployeeStateInsurance);
        Assert.Equal(200m, r.ProfessionalTax);
        Assert.Equal(0m, r.TaxDeductedAtSource);
        Assert.Equal(16_801m, r.NetPay);
    }

    [Fact]
    public void E2E_OT_NightPremium_TaxKicksIn()
    {
        // salary 26000; 4 OT hrs at 150/hr * 1.5 = 900; night premium on 13 days * 30% = 3900
        // gross = 26000 + 900 + 3900 = 30800
        // basicPay = 26000 * 40% = 10400; PF = 10400 * 12% = 1248
        // PT = KA, 30800 > 18000 → 200
        // ESI: 30800 > 21000 → 0
        // annual = 30800 * 12 = 369600; taxable = 369600 - 75000 = 294600 < 300k → TDS = 0
        // net = 30800 - 1248 - 200 = 29352
        var att = Days(13, shiftStart: new TimeOnly(22, 0))
            .Concat(Days(11, startDate: new DateOnly(2025, 4, 14)))
            .Concat(Days(2, hours: 10m, startDate: new DateOnly(2025, 4, 25)))
            .ToList();
        var r = Calc(Inp(salary: 26_000m, hourlyRate: 150m, att: att,
            ot: OtPolicy((0, null, 1.5m, 1)),
            premiums: [NightPremium(30m)]));
        Assert.Equal(900m, r.OvertimePay);
        Assert.Equal(3_900m, r.ShiftPremium);
        Assert.Equal(30_800m, r.GrossPay);
        Assert.Equal(0m, r.TaxDeductedAtSource);
        Assert.Equal(29_352m, r.NetPay);
    }
}
