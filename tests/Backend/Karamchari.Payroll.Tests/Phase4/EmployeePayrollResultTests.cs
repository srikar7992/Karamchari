using Karamchari.Payroll.Domain.Results;

namespace Karamchari.Payroll.Tests.Phase4;

/// <summary>
/// Proves EmployeePayrollResult invariants in domain code, not documentation.
/// Covers: invariant guards, state machine transitions, domain events.
/// </summary>
public class EmployeePayrollResultTests
{
    private static readonly Guid RunId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    private static EarningsBreakdown DefaultEarnings(
        decimal @base = 50_000,
        decimal ot = 0,
        decimal holiday = 0,
        decimal premium = 0,
        decimal allowance = 0,
        decimal bonus = 0) =>
        new(@base, ot, holiday, premium, allowance, bonus);

    private static DeductionsBreakdown DefaultDeductions(
        decimal pf = 1_800,
        decimal esi = 0,
        decimal pt = 200,
        decimal tds = 5_000,
        decimal other = 0) =>
        new(pf, esi, pt, tds, other);

    private static EmployeePayrollResult ValidResult(
        EarningsBreakdown? earnings = null,
        DeductionsBreakdown? deductions = null,
        decimal retro = 0m,
        decimal manual = 0m) =>
        EmployeePayrollResult.Calculate(
            "tenant1", RunId, EmployeeId, "Ravi Kumar", "May 2026", 1, "snap-1",
            earnings ?? DefaultEarnings(),
            deductions ?? DefaultDeductions(),
            retro, manual);

    // ──────────────────────────────────────────────────────────────────────
    // Invariant guards at Calculate()
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_ValidInputs_CreatesResult()
    {
        var result = ValidResult();
        Assert.Equal(EmployeePayrollResultStatus.Calculated, result.Status);
        Assert.Equal(EmployeeId, result.EmployeeId);
        Assert.Equal(RunId, result.PayrollRunId);
    }

    [Fact]
    public void Calculate_GrossAndNetSet_Correctly()
    {
        // GrossPay = base 50k + OT 5k = 55k; deductions = 1800+200+5000=7000; net = 48k
        var e = DefaultEarnings(@base: 50_000, ot: 5_000);
        var d = new DeductionsBreakdown(1_800, 0, 200, 5_000, 0);
        var result = EmployeePayrollResult.Calculate(
            "t1", RunId, EmployeeId, "Test", "May 2026", 1, "s1", e, d, 0, 0);
        Assert.Equal(55_000m, result.GrossPay);
        Assert.Equal(7_000m, result.TotalDeductions);
        Assert.Equal(48_000m, result.NetPay);
    }

    [Fact]
    public void Calculate_NegativeBasePay_Throws()
    {
        var e = DefaultEarnings(@base: -1);
        Assert.Throws<InvalidOperationException>(() => ValidResult(earnings: e));
    }

    [Fact]
    public void Calculate_NegativeOvertimePay_Throws()
    {
        var e = DefaultEarnings(ot: -100);
        Assert.Throws<InvalidOperationException>(() => ValidResult(earnings: e));
    }

    [Fact]
    public void Calculate_NegativePF_Throws()
    {
        var d = new DeductionsBreakdown(-100, 0, 0, 0, 0);
        Assert.Throws<InvalidOperationException>(() => ValidResult(deductions: d));
    }

    [Fact]
    public void Calculate_NegativeTDS_Throws()
    {
        var d = new DeductionsBreakdown(0, 0, 0, -100, 0);
        Assert.Throws<InvalidOperationException>(() => ValidResult(deductions: d));
    }

    [Fact]
    public void Calculate_NetPayNegative_Throws()
    {
        // Deductions exceed gross
        var e = DefaultEarnings(@base: 10_000);
        var d = new DeductionsBreakdown(1_800, 0, 0, 50_000, 0); // TDS 50k > gross 10k
        Assert.Throws<InvalidOperationException>(() =>
            EmployeePayrollResult.Calculate("t1", RunId, EmployeeId, "X", "May 2026", 1, "s1", e, d, 0, 0));
    }

    [Fact]
    public void Calculate_RetroAdjustment_IncludedInNetPay()
    {
        // Net = gross - deductions + retro
        var result = ValidResult(retro: 1_250m);
        var expected = result.GrossPay - result.TotalDeductions + 1_250m;
        Assert.Equal(expected, result.NetPay);
    }

    // ──────────────────────────────────────────────────────────────────────
    // State transitions
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Approve_FromCalculated_Succeeds()
    {
        var result = ValidResult();
        result.Approve("hr@company.com");
        Assert.Equal(EmployeePayrollResultStatus.Approved, result.Status);
        Assert.Equal("hr@company.com", result.ApprovedBy);
        Assert.NotNull(result.ApprovedAtUtc);
    }

    [Fact]
    public void Lock_FromApproved_Succeeds()
    {
        var result = ValidResult();
        result.Approve("hr@company.com");
        result.Lock();
        Assert.Equal(EmployeePayrollResultStatus.Locked, result.Status);
        Assert.NotNull(result.LockedAtUtc);
    }

    [Fact]
    public void Approve_FromApproved_Throws()
    {
        var result = ValidResult();
        result.Approve("hr@company.com");
        Assert.Throws<InvalidOperationException>(() => result.Approve("hr2@company.com"));
    }

    [Fact]
    public void Approve_FromLocked_Throws()
    {
        var result = ValidResult();
        result.Approve("hr@company.com");
        result.Lock();
        Assert.Throws<InvalidOperationException>(() => result.Approve("hr3@company.com"));
    }

    [Fact]
    public void Lock_FromCalculated_Throws()
    {
        var result = ValidResult();
        Assert.Throws<InvalidOperationException>(() => result.Lock());
    }

    [Fact]
    public void Lock_Idempotent_WhenAlreadyLocked()
    {
        var result = ValidResult();
        result.Approve("hr@company.com");
        result.Lock();
        // Second lock call should not throw (guard: if already locked, return)
        result.Lock();
        Assert.Equal(EmployeePayrollResultStatus.Locked, result.Status);
    }

    [Fact]
    public void Approve_EmptyApprover_Throws()
    {
        var result = ValidResult();
        Assert.Throws<ArgumentException>(() => result.Approve(""));
        Assert.Throws<ArgumentException>(() => result.Approve("   "));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Domain events
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_RaisesEmployeePayrollCalculatedEvent()
    {
        var result = ValidResult();
        Assert.Contains(result.DomainEvents,
            e => e.GetType().Name == "EmployeePayrollCalculatedEvent");
    }

    [Fact]
    public void Approve_RaisesEmployeePayrollApprovedEvent()
    {
        var result = ValidResult();
        result.ClearDomainEvents();
        result.Approve("hr@company.com");
        Assert.Contains(result.DomainEvents,
            e => e.GetType().Name == "EmployeePayrollApprovedEvent");
    }

    [Fact]
    public void Lock_RaisesEmployeePayrollLockedEvent()
    {
        var result = ValidResult();
        result.Approve("hr@company.com");
        result.ClearDomainEvents();
        result.Lock();
        Assert.Contains(result.DomainEvents,
            e => e.GetType().Name == "EmployeePayrollLockedEvent");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Immutability — no public mutation path after creation
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void GrossPay_DoesNotChange_AfterApproval()
    {
        var result = ValidResult();
        var grossBefore = result.GrossPay;
        result.Approve("hr@company.com");
        Assert.Equal(grossBefore, result.GrossPay);
    }

    [Fact]
    public void NetPay_DoesNotChange_AfterLock()
    {
        var result = ValidResult();
        var netBefore = result.NetPay;
        result.Approve("hr@company.com");
        result.Lock();
        Assert.Equal(netBefore, result.NetPay);
    }

    [Fact]
    public void TotalDeductions_DoesNotChange_AfterApproval()
    {
        var result = ValidResult();
        var deductionsBefore = result.TotalDeductions;
        result.Approve("hr@company.com");
        Assert.Equal(deductionsBefore, result.TotalDeductions);
    }

    // ──────────────────────────────────────────────────────────────────────
    // CalculatedAtUtc stamped correctly
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void CalculatedAtUtc_Set_OnCreation()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var result = ValidResult();
        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        Assert.True(result.CalculatedAtUtc >= before && result.CalculatedAtUtc <= after);
    }

    [Fact]
    public void ApprovedAtUtc_Set_OnApproval()
    {
        var result = ValidResult();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        result.Approve("hr@company.com");
        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        Assert.NotNull(result.ApprovedAtUtc);
        Assert.True(result.ApprovedAtUtc >= before && result.ApprovedAtUtc <= after);
    }

    [Fact]
    public void LockedAtUtc_Set_OnLock()
    {
        var result = ValidResult();
        result.Approve("hr@company.com");
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        result.Lock();
        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        Assert.NotNull(result.LockedAtUtc);
        Assert.True(result.LockedAtUtc >= before && result.LockedAtUtc <= after);
    }
}
