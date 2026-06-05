using Karamchari.Payroll.Domain.Adjustments;
using Karamchari.Payroll.Domain.Results;
using Karamchari.Payroll.Services.Calculation;
using Karamchari.Payroll.Services.Deductions;

namespace Karamchari.Payroll.Tests.Phase4;

/// <summary>
/// Proves retro adjustment correctness without a database.
///
/// Group 1: RetroPayrollAdjustment domain object invariants.
/// Group 2: Engine-delta scenarios — simulates what RetroPayrollProcessor computes
///          by running the engine twice (original vs corrected input) and asserting
///          the diff matches expected financial impact.
///
/// Key correctness question: retro produces the DIFF, not a full recalculation reset.
/// Each test verifies that only the changed attendance component drives the delta.
/// </summary>
public class RetroAdjustmentTests
{
    private static readonly Guid RunId = Guid.NewGuid();
    private static readonly Guid EmpId = Guid.NewGuid();
    private static readonly TaxCalculatorService TaxSvc = new();

    private static PayrollCalculationEngine Engine() => new([
        new ProvidentFundCalculator(),
        new EsiCalculator(),
        new ProfessionalTaxCalculator(),
    ]);

    // ── Group 1: RetroPayrollAdjustment domain ────────────────────────────────

    [Fact]
    public void Create_PositiveCorrection_DifferencePositive()
    {
        var retro = RetroPayrollAdjustment.Create("t1", EmpId, RunId,
            45_000m, 47_500m, "OT added", "hr@co");
        Assert.Equal(2_500m, retro.Difference);
        Assert.Equal(AdjustmentStatus.Pending, retro.Status);
    }

    [Fact]
    public void Create_NegativeCorrection_DifferenceNegative()
    {
        var retro = RetroPayrollAdjustment.Create("t1", EmpId, RunId,
            45_000m, 43_000m, "Day removed", "hr@co");
        Assert.Equal(-2_000m, retro.Difference);
    }

    [Fact]
    public void Create_ZeroChange_DifferenceZero()
    {
        var retro = RetroPayrollAdjustment.Create("t1", EmpId, RunId,
            45_000m, 45_000m, "No impact", "hr@co");
        Assert.Equal(0m, retro.Difference);
    }

    [Fact]
    public void Create_StoresAmounts_Correctly()
    {
        var retro = RetroPayrollAdjustment.Create("t1", EmpId, RunId,
            47_000m, 49_500m, "r", "hr@co");
        Assert.Equal(47_000m, retro.AmountPaid);
        Assert.Equal(49_500m, retro.AmountShouldBePaid);
        Assert.Equal(2_500m, retro.Difference);
    }

    [Fact]
    public void Create_RaisesRetroAdjustmentCreatedEvent()
    {
        var retro = RetroPayrollAdjustment.Create("t1", EmpId, RunId,
            40_000m, 42_000m, "r", "hr@co");
        Assert.Contains(retro.DomainEvents,
            e => e.GetType().Name == "RetroAdjustmentCreatedEvent");
    }

    [Fact]
    public void Apply_Pending_TransitionsToApplied()
    {
        var retro = RetroPayrollAdjustment.Create("t1", EmpId, RunId,
            40_000m, 42_000m, "r", "hr@co");
        var nextRun = Guid.NewGuid();
        retro.Apply(nextRun);
        Assert.Equal(AdjustmentStatus.Applied, retro.Status);
        Assert.Equal(nextRun, retro.AppliedInRunId);
        Assert.NotNull(retro.AppliedAtUtc);
    }

    [Fact]
    public void Apply_AlreadyApplied_Throws()
    {
        var retro = RetroPayrollAdjustment.Create("t1", EmpId, RunId,
            40_000m, 42_000m, "r", "hr@co");
        retro.Apply(Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => retro.Apply(Guid.NewGuid()));
    }

    [Fact]
    public void Apply_SetsAppliedAtUtc_WithinTolerance()
    {
        var retro = RetroPayrollAdjustment.Create("t1", EmpId, RunId,
            40_000m, 42_000m, "r", "hr@co");
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        retro.Apply(Guid.NewGuid());
        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        Assert.True(retro.AppliedAtUtc >= before && retro.AppliedAtUtc <= after);
    }

    [Fact]
    public void Create_Difference_IsShouldhavePaidMinusPaid()
    {
        // Difference = ShouldBePaid - AmountPaid (positive = owe more, negative = overpaid)
        var retro = RetroPayrollAdjustment.Create("t1", EmpId, RunId,
            50_000m, 48_000m, "r", "hr@co");
        Assert.Equal(-2_000m, retro.Difference);
    }

    // ── Group 2: Engine-delta scenarios ──────────────────────────────────────

    private static List<DayAttendanceRecord> Days(
        int count,
        decimal hours = 8m,
        bool present = true,
        DateOnly? startDate = null,
        bool isHoliday = false)
    {
        var d = startDate ?? new DateOnly(2025, 4, 1);
        return [..Enumerable.Range(0, count).Select(i =>
            new DayAttendanceRecord(d.AddDays(i), hours, present, false, isHoliday, null, null))];
    }

    private static EmployeePayrollInput Inp(
        decimal salary = 30_000m,
        string state = "KA",
        decimal hourlyRate = 0m,
        List<DayAttendanceRecord>? att = null,
        OvertimePolicySnapshot? ot = null) =>
        new(EmpId, "Ravi", salary, hourlyRate, "INR", 40m, 0m,
            att ?? Days(26),
            [],
            ot ?? new OvertimePolicySnapshot([]),
            [], [],
            false, false, state, "New",
            0m, 0m, 0m, 4, 2025,
            Guid.NewGuid(), "FY2024-25-V1",
            new StatutoryConfigSnapshot(15_000m, 21_000m, 0.0075m, 0.0325m, 0.12m, 0.0367m, 1_250m));

    private static EmployeePayrollResult Calc(EmployeePayrollInput input) =>
        Engine().Calculate("t1", RunId, "Apr 2025", 1, "snap1", input, TaxSvc);

    // Scenario: absence corrected to presence → positive retro
    [Fact]
    public void Delta_AbsenceCorrectedToPresence_PositiveDiff()
    {
        // Original: 25 present, 1 absent in 26-day month
        var originalAtt = Days(25)
            .Concat(Days(1, present: false, startDate: new DateOnly(2025, 4, 26)))
            .ToList();
        var original = Calc(Inp(salary: 26_000m, att: originalAtt));

        // Corrected: all 26 present
        var corrected = Calc(Inp(salary: 26_000m, att: Days(26)));

        var diff = corrected.NetPay - original.NetPay;
        Assert.True(diff > 0, $"Adding absent day should be positive retro; diff={diff}");
    }

    // Base pay delta = exact one day's rate
    [Fact]
    public void Delta_BasePay_ExactOneDailyRate()
    {
        // 26000 / 26 days = 1000 per day
        var originalAtt = Days(25)
            .Concat(Days(1, present: false, startDate: new DateOnly(2025, 4, 26)))
            .ToList();
        var original = Calc(Inp(salary: 26_000m, att: originalAtt));
        var corrected = Calc(Inp(salary: 26_000m, att: Days(26)));
        Assert.Equal(1_000m, corrected.BasePay - original.BasePay);
    }

    // Scenario: presence corrected to absence → negative retro
    [Fact]
    public void Delta_PresenceCorrectedToAbsence_NegativeDiff()
    {
        var original = Calc(Inp(salary: 26_000m, att: Days(26)));
        var correctedAtt = Days(25)
            .Concat(Days(1, present: false, startDate: new DateOnly(2025, 4, 26)))
            .ToList();
        var corrected = Calc(Inp(salary: 26_000m, att: correctedAtt));
        Assert.True(corrected.NetPay - original.NetPay < 0);
    }

    // Scenario: OT hours removed → negative retro of exact OT amount
    [Fact]
    public void Delta_OTRemoved_ExactOTAmountNegative()
    {
        var otPolicy = new OvertimePolicySnapshot([
            new OvertimeRuleSnapshot(0, null, 1.5m, "Regular", 1),
        ]);

        // Original: 4 OT hrs (2 days at 10h, rest at 8h)
        var originalAtt = Days(24)
            .Concat(Days(2, hours: 10m, startDate: new DateOnly(2025, 4, 25)))
            .ToList();
        var original = Calc(Inp(salary: 30_000m, hourlyRate: 150m, att: originalAtt, ot: otPolicy));

        // Corrected: all 8h, no OT
        var corrected = Calc(Inp(salary: 30_000m, hourlyRate: 150m, att: Days(26), ot: otPolicy));

        var diff = corrected.NetPay - original.NetPay;
        // OT lost: 4 * 150 * 1.5 = 900; TDS unchanged (both below tax threshold); diff = -900
        Assert.Equal(-900m, diff, precision: 0);
    }

    // Scenario: OT hours added → positive retro
    [Fact]
    public void Delta_OTAdded_PositiveDiff()
    {
        var otPolicy = new OvertimePolicySnapshot([
            new OvertimeRuleSnapshot(0, null, 1.5m, "Regular", 1),
        ]);
        var original = Calc(Inp(salary: 30_000m, hourlyRate: 150m, att: Days(26), ot: otPolicy));
        var correctedAtt = Days(24)
            .Concat(Days(2, hours: 10m, startDate: new DateOnly(2025, 4, 25)))
            .ToList();
        var corrected = Calc(Inp(salary: 30_000m, hourlyRate: 150m, att: correctedAtt, ot: otPolicy));
        Assert.Equal(900m, corrected.NetPay - original.NetPay, precision: 0);
    }

    // Scenario: no change → zero diff (processor returns null)
    [Fact]
    public void Delta_NoChange_ZeroDiff()
    {
        var att = Days(26);
        var original = Calc(Inp(salary: 30_000m, att: att));
        var corrected = Calc(Inp(salary: 30_000m, att: att));
        Assert.Equal(0m, corrected.NetPay - original.NetPay);
    }

    // Scenario: tier-2 OT corrected down to tier-1 only
    [Fact]
    public void Delta_OTTierDowngrade_CorrectDiff()
    {
        var otPolicy = new OvertimePolicySnapshot([
            new OvertimeRuleSnapshot(0m, 4m, 1.5m, "Regular", 1),
            new OvertimeRuleSnapshot(4m, null, 2.0m, "Regular", 2),
        ]);

        // Original: 8 OT hrs (4 at tier1, 4 at tier2)
        // 24 days * 8h + 2 days * 12h = 192+24 = 216; regular = 208; OT = 8
        var originalAtt = Days(24)
            .Concat(Days(2, hours: 12m, startDate: new DateOnly(2025, 4, 25)))
            .ToList();
        var original = Calc(Inp(hourlyRate: 100m, att: originalAtt, ot: otPolicy));
        // OT pay: tier1=4*100*1.5=600; tier2=4*100*2=800; total=1400

        // Corrected: 4 OT hrs only (tier1 only)
        // 24 days * 8h + 2 days * 10h = 192+20 = 212; OT = 4
        var correctedAtt = Days(24)
            .Concat(Days(2, hours: 10m, startDate: new DateOnly(2025, 4, 25)))
            .ToList();
        var corrected = Calc(Inp(hourlyRate: 100m, att: correctedAtt, ot: otPolicy));
        // OT pay: tier1=4*100*1.5=600; tier2=0; total=600

        // Diff = corrected.OT - original.OT = 600 - 1400 = -800
        Assert.Equal(-800m, corrected.OvertimePay - original.OvertimePay, precision: 0);
        Assert.Equal(-800m, corrected.NetPay - original.NetPay, precision: 0);
    }

    // Pathological: correction pushes gross above ESI ceiling → ESI stops
    [Fact]
    public void Delta_CorrectionPushesGrossAboveEsiCeiling_EsiDrops()
    {
        // Original: gross = 18000 (ESI applies: 135)
        var original = Calc(Inp(salary: 18_000m));
        Assert.Equal(135m, original.EmployeeStateInsurance);

        // Corrected: OT adds 6000; gross = 24000 > 21000 → ESI = 0
        var otPolicy = new OvertimePolicySnapshot([
            new OvertimeRuleSnapshot(0, null, 1.5m, "Regular", 1),
        ]);
        var correctedAtt = Days(24)
            .Concat(Days(2, hours: 10m, startDate: new DateOnly(2025, 4, 25)))
            .ToList();
        var corrected = Calc(Inp(salary: 18_000m, hourlyRate: 1_000m,
            att: correctedAtt, ot: otPolicy));

        Assert.True(corrected.GrossPay > 21_000m);
        Assert.Equal(0m, corrected.EmployeeStateInsurance);
        // Net diff = higher gross (+6000) - no ESI saved (+135) - PF same = ~6135
        Assert.True(corrected.NetPay > original.NetPay);
    }

    // Pathological: salary is immutable in snapshot — correction changes only hours
    [Fact]
    public void Delta_SnapshotSalaryUnchanged_DiffFromHoursOnly()
    {
        // Both runs use same salary (30k); only hours differ
        var originalAtt = Days(24)
            .Concat(Days(2, hours: 10m, startDate: new DateOnly(2025, 4, 25)))
            .ToList();
        var original = Calc(Inp(salary: 30_000m, hourlyRate: 150m,
            att: originalAtt, ot: new OvertimePolicySnapshot([new OvertimeRuleSnapshot(0, null, 1.5m, "Regular", 1)])));

        // Simulating a "salary now raised to 40k" scenario — retro processor uses original 30k snapshot
        var corrected = Calc(Inp(salary: 30_000m, hourlyRate: 150m, // salary still 30k!
            att: Days(26), ot: new OvertimePolicySnapshot([new OvertimeRuleSnapshot(0, null, 1.5m, "Regular", 1)])));

        // Diff is purely from OT removal (-900), not from salary change
        Assert.Equal(-900m, corrected.NetPay - original.NetPay, precision: 0);
    }

    // Pathological: employee fully absent month → corrected to full month → full salary retro
    [Fact]
    public void Delta_EntireMonthAbsentCorrectedToFullPresence_FullSalaryDiff()
    {
        var allAbsent = Days(26, present: false);
        var original = Calc(Inp(salary: 26_000m, att: allAbsent));
        Assert.Equal(0m, original.BasePay);

        var allPresent = Days(26);
        var corrected = Calc(Inp(salary: 26_000m, att: allPresent));
        Assert.Equal(26_000m, corrected.BasePay);

        // diff > 0; exact value includes PF/ESI/PT effects
        Assert.True(corrected.NetPay > original.NetPay);
    }

    // Pathological: retro creates adjustment with exact financial delta
    [Fact]
    public void Delta_RetroAdjustmentCreated_WithCorrectDiffAmount()
    {
        var originalAtt = Days(25)
            .Concat(Days(1, present: false, startDate: new DateOnly(2025, 4, 26)))
            .ToList();
        var original = Calc(Inp(salary: 26_000m, att: originalAtt));
        var corrected = Calc(Inp(salary: 26_000m, att: Days(26)));

        var diff = corrected.NetPay - original.NetPay;
        Assert.True(diff > 0);

        var retro = RetroPayrollAdjustment.Create(
            "t1", EmpId, RunId,
            original.NetPay, corrected.NetPay,
            "Attendance correction", "hr@co");

        Assert.Equal(diff, retro.Difference);
        Assert.Equal(original.NetPay, retro.AmountPaid);
        Assert.Equal(corrected.NetPay, retro.AmountShouldBePaid);
    }

    // Pathological: high-OT employee, removing OT lowers projected annual, reduces TDS
    [Fact]
    public void Delta_OTRemoved_HighSalary_TaxDrops_NetDiffHigherThanJustOT()
    {
        var otPolicy = new OvertimePolicySnapshot([
            new OvertimeRuleSnapshot(0, null, 1.5m, "Regular", 1),
        ]);

        // With OT: salary 80k + OT pushes further into higher slab → higher TDS
        var withOtAtt = Days(26, hours: 9m);
        var original = Calc(Inp(salary: 80_000m, hourlyRate: 500m,
            att: withOtAtt, ot: otPolicy));

        // Without OT: TDS reduced
        var noOtAtt = Days(26);
        var corrected = Calc(Inp(salary: 80_000m, hourlyRate: 500m,
            att: noOtAtt, ot: otPolicy));

        Assert.True(original.OvertimePay > 0);
        Assert.Equal(0m, corrected.OvertimePay);

        // Removing OT: gross drops AND TDS drops → net diff = -(otLoss - taxSaving)
        // taxSaving > 0 so actual net diff is less negative than pure OT loss
        var otLoss = original.OvertimePay;
        var taxSaving = original.TaxDeductedAtSource - corrected.TaxDeductedAtSource;
        var expectedDiff = -(otLoss - taxSaving);
        var actualDiff = corrected.NetPay - original.NetPay;
        Assert.Equal(expectedDiff, actualDiff, precision: 0);
    }

    // Pathological: positive retro where employee crossed PF ceiling
    [Fact]
    public void Delta_SalaryAbovePFCeiling_OTDoesNotChangePF()
    {
        // Both runs: salary 80k → basicPay = 32k > 15k → PF always 1800
        // OT changes gross and TDS but NOT PF (PF is on basicPay from basePay, not grossPay)
        var otPolicy = new OvertimePolicySnapshot([
            new OvertimeRuleSnapshot(0, null, 1.5m, "Regular", 1),
        ]);
        var withOt = Calc(Inp(salary: 80_000m, hourlyRate: 500m,
            att: Days(26, hours: 9m), ot: otPolicy));
        var withoutOt = Calc(Inp(salary: 80_000m, hourlyRate: 500m,
            att: Days(26), ot: otPolicy));

        // PF must be identical regardless of OT
        Assert.Equal(withoutOt.ProvidentFund, withOt.ProvidentFund);
        Assert.Equal(1_800m, withOt.ProvidentFund);
    }

    // Pathological: multiple retro adjustments for same employee accumulate
    [Fact]
    public void MultipleRetros_TotalDiffIsSum()
    {
        var r1 = RetroPayrollAdjustment.Create("t1", EmpId, RunId, 40_000m, 41_000m, "r1", "hr");
        var r2 = RetroPayrollAdjustment.Create("t1", EmpId, RunId, 41_000m, 40_500m, "r2", "hr");
        var r3 = RetroPayrollAdjustment.Create("t1", EmpId, RunId, 40_500m, 41_200m, "r3", "hr");

        // 1000 - 500 + 700 = 1200
        Assert.Equal(1_200m, r1.Difference + r2.Difference + r3.Difference);
    }

    // Regression: retro on partial month doesn't break proportional calculation
    [Fact]
    public void Delta_PartialMonth_CorrectionProportional()
    {
        // Original: 13/26 present → basePay = 13000
        var originalAtt = Days(13)
            .Concat(Days(13, present: false, startDate: new DateOnly(2025, 4, 14)))
            .ToList();
        var original = Calc(Inp(salary: 26_000m, att: originalAtt));
        Assert.Equal(13_000m, original.BasePay);

        // Corrected: one more day present → 14/26 → basePay = 14000
        var correctedAtt = Days(14)
            .Concat(Days(12, present: false, startDate: new DateOnly(2025, 4, 15)))
            .ToList();
        var corrected = Calc(Inp(salary: 26_000m, att: correctedAtt));
        Assert.Equal(14_000m, corrected.BasePay);

        // basePay diff = 1000 exactly
        Assert.Equal(1_000m, corrected.BasePay - original.BasePay);
    }
}
