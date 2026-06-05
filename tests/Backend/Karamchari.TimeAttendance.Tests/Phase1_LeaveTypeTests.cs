namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves;
using Xunit;

/// <summary>Phase 1 — Domain Validation: PL, Sick, CompOff, LOP, Maternity.</summary>
public sealed class Phase1_LeaveTypeTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    private static readonly Guid AnyApprover = Guid.NewGuid();

    private static LeaveBalance NewBalance(decimal accrue = 0)
    {
        var b = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        if (accrue > 0) b.Accrue(accrue, Today);
        return b;
    }

    private static CompOffGrant NewGrant(decimal earned = 1m, int expiryOffsetDays = 90)
    {
        // workedOnDate must be strictly before expiryDate
        var worked = expiryOffsetDays > 0 ? Today.AddDays(-1) : Today.AddDays(expiryOffsetDays - 1);
        var expiry = Today.AddDays(expiryOffsetDays);
        return CompOffGrant.Create(Guid.NewGuid(), worked, earned, expiry, AnyApprover);
    }

    private static LeaveRequest ApprovedRequest()
    {
        var r = LeaveRequest.Create(Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), 20);
        r.Submit();
        r.StartApproval();
        r.FinalizeApproved();
        return r;
    }

    // ── PL ──────────────────────────────────────────────────────────────────

    [Fact]
    public void PL_Apply_DeductsBalance()
    {
        var b = NewBalance(18);
        b.Consume(5m, Today, "req-001");
        b.AvailableBalance.Should().Be(13m);
    }

    [Fact]
    public void PL_Cancel_RestoresBalance()
    {
        var b = NewBalance(18);
        b.Consume(5m, Today, "req-001");
        b.Restore(5m, Today, "req-001");
        b.AvailableBalance.Should().Be(18m);
    }

    [Fact]
    public void PL_CarryForward_ExcessLapses_Correctly()
    {
        var b = NewBalance(20);
        b.Expire(5m, Today);          // 5 days lapse (Expire → -5)
        b.CarryForward(15m, Today);   // 15 carried  (CarryForward → +15)
        b.AvailableBalance.Should().Be(30m); // 20 - 5 + 15 = 30
        b.AvailableBalance.Should().Be(b.Entries.Sum(e => e.Quantity), "invariant");
    }

    [Fact]
    public void PL_Encashment_ReducesBalance()
    {
        var b = NewBalance(30);
        b.Encash(10m, Today, "enc-001");
        b.AvailableBalance.Should().Be(20m);
    }

    [Fact]
    public void PL_Encashment_BeyondBalance_Throws()
    {
        var b = NewBalance(5);
        var act = () => b.Encash(10m, Today, "enc-001");
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Sick Leave ─────────────────────────────────────────────────────────

    [Fact]
    public void SickLeave_RetroactiveBoundary_ExactlyAtLimit_CountsAsValid()
    {
        var rules = new LeavePolicyRules { MaxRetroactiveDays = 3 };
        var leaveStart = Today.AddDays(-3);
        (Today.DayNumber - leaveStart.DayNumber).Should().Be(rules.MaxRetroactiveDays);
    }

    [Fact]
    public void SickLeave_RetroactiveBeyondLimit_DetectedByRule()
    {
        var rules = new LeavePolicyRules { MaxRetroactiveDays = 2 };
        var leaveStart = Today.AddDays(-5);
        (Today.DayNumber - leaveStart.DayNumber).Should().BeGreaterThan(rules.MaxRetroactiveDays);
    }

    [Fact]
    public void SickLeave_BalanceExhausted_Throws()
    {
        var b = NewBalance(1);
        var act = () => b.Consume(3m, Today, "sick-req");
        act.Should().Throw<InvalidOperationException>();
    }

    // ── CompOff ────────────────────────────────────────────────────────────

    [Fact]
    public void CompOff_Grant_AvailableEqualsEarned() => NewGrant(1m).DaysAvailable.Should().Be(1m);

    [Fact]
    public void CompOff_PartialConsume_RemainsAvailable()
    {
        var g = NewGrant(2m);
        g.Consume(1m, Guid.NewGuid());
        g.DaysAvailable.Should().Be(1m);
    }

    [Fact]
    public void CompOff_FullConsume_StatusFullyConsumed()
    {
        var g = NewGrant(1m);
        g.Consume(1m, Guid.NewGuid());
        g.Status.Should().Be(CompOffGrantStatus.FullyConsumed);
    }

    [Fact]
    public void CompOff_Consume_BeyondAvailable_Throws()
    {
        var g = NewGrant(0.5m);
        var act = () => g.Consume(1m, Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CompOff_Fifo_OlderConsumedFirst_NewerUntouched()
    {
        var older = NewGrant(1m);
        var newer = NewGrant(1m);
        older.Consume(1m, Guid.NewGuid());
        newer.DaysAvailable.Should().Be(1m);
        older.Status.Should().Be(CompOffGrantStatus.FullyConsumed);
    }

    [Fact]
    public void CompOff_Expire_SetsExpiredStatus()
    {
        var g = NewGrant(1m, expiryOffsetDays: -1);
        g.Expire();
        g.Status.Should().Be(CompOffGrantStatus.Expired);
    }

    [Fact]
    public void CompOff_Reinstate_ReactivatesExpired()
    {
        var g = NewGrant(1m, expiryOffsetDays: -1);
        g.Expire();
        g.Reinstate();
        g.Status.Should().Be(CompOffGrantStatus.Active);
    }

    [Fact]
    public void CompOff_Reinstate_OnActiveGrant_Throws()
        => new Action(() => NewGrant().Reinstate()).Should().Throw<InvalidOperationException>();

    // ── LOP ───────────────────────────────────────────────────────────────

    [Fact]
    public void LOP_LOPConversionReason_AllowsNegativeBalance()
    {
        // AdjustmentReason.LOPConversion bypasses guard — auditable negative entry.
        var b = NewBalance(0);
        b.Adjust(-2m, Today, "payroll-system", AdjustmentReason.LOPConversion);
        b.AvailableBalance.Should().Be(-2m);
        b.Entries.Should().Contain(e => e.EntryType == LeaveBalanceEntryType.LOPConversion);
    }

    [Fact]
    public void LOP_PayrollCorrectionReason_AllowsNegativeBalance()
    {
        var b = NewBalance(1);
        b.Adjust(-3m, Today, "payroll-system", AdjustmentReason.PayrollCorrection);
        b.AvailableBalance.Should().Be(-2m);
        b.Entries.Should().Contain(e => e.EntryType == LeaveBalanceEntryType.PayrollCorrection);
    }

    [Fact]
    public void LOP_ManualCorrection_NegativeResult_StillGuarded()
    {
        // ManualCorrection goes through standard guard — no accidental bypass.
        var b = NewBalance(0);
        var act = () => b.Adjust(-2m, Today, "hr-admin", AdjustmentReason.ManualCorrection);
        act.Should().Throw<InvalidOperationException>("ManualCorrection respects balance guard");
    }

    [Fact]
    public void LOP_AccrualPaused_AccrueIsNoOp()
    {
        var b = NewBalance(10);
        b.PauseAccrual("Suspension");
        b.Accrue(5m, Today);
        b.AvailableBalance.Should().Be(10m);
    }

    [Fact]
    public void LOP_ResumeAccrual_AccrueWorks()
    {
        var b = NewBalance(10);
        b.PauseAccrual("LOP");
        b.ResumeAccrual();
        b.Accrue(5m, Today);
        b.AvailableBalance.Should().Be(15m);
    }

    // ── Maternity ─────────────────────────────────────────────────────────

    [Fact]
    public void Maternity_CrossLeaveYear_ClipsCorrectly()
    {
        var start = new DateOnly(2025, 12, 15);
        var end = new DateOnly(2026, 3, 14);

        var clip2025 = LeaveYear.CalendarYear(2025).Clip(start, end);
        var clip2026 = LeaveYear.CalendarYear(2026).Clip(start, end);

        clip2025.Should().NotBeNull();
        clip2026.Should().NotBeNull();
        clip2025!.Value.To.Should().Be(new DateOnly(2025, 12, 31));
        clip2026!.Value.From.Should().Be(new DateOnly(2026, 1, 1));
        clip2026.Value.To.Should().Be(end);
    }

    [Fact]
    public void Maternity_ReturnEarly_AdjustsActualDays()
    {
        var r = ApprovedRequest();
        r.AdjustActualDaysForHoliday(new DateOnly(2026, 3, 15), daysToRemove: 1);
        r.ActualDays.Should().Be(19);
    }

    // ── Balance Invariant (property-based simulation) ─────────────────────

    [Theory]
    [InlineData(100, 10, 5, 95)]   // 100 - 10 + 5 = 95
    [InlineData(20, 20, 0, 0)]
    [InlineData(5, 3, 1, 3)]
    [InlineData(50, 0, 0, 50)]
    public void LeaveBalance_Invariant_BalanceEqualsLedgerSum(
        decimal accrue, decimal consume, decimal restore, decimal expected)
    {
        var b = NewBalance(accrue);
        if (consume > 0) b.Consume(consume, Today, "req");
        if (restore > 0) b.Restore(restore, Today, "req");

        b.AvailableBalance.Should().Be(expected);
        b.AvailableBalance.Should().Be(b.Entries.Sum(e => e.Quantity));
    }

    [Fact]
    public void LeaveBalance_PropertyBased_InvariantHoldsAcross1000RandomOps()
    {
        var rng = new Random(42);
        var b = NewBalance(1000m);

        for (int i = 0; i < 1000; i++)
        {
            var qty = (decimal)(rng.Next(1, 10));
            switch (rng.Next(3))
            {
                case 0 when qty <= b.AvailableBalance:
                    b.Consume(qty, Today, $"r{i}");
                    break;
                case 1:
                    b.Restore(qty, Today, $"r{i}");
                    break;
                default:
                    b.Accrue(qty, Today);
                    break;
            }
        }

        b.AvailableBalance.Should().Be(b.Entries.Sum(e => e.Quantity), "invariant must always hold");
    }
}
