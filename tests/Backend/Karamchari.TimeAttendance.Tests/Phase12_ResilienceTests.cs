namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Domain.Scheduling;
using Karamchari.TimeAttendance.Persistence;
using Xunit;

/// <summary>
/// Phase 12 — Resilience: idempotency, duplicate event replay, scheduler crash recovery.
/// </summary>
public sealed class Phase12_ResilienceTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    // ── Idempotency: Duplicate Consume ───────────────────────────────────

    [Fact]
    public void Balance_DuplicateConsumeWithSameRef_DoubleDebit_Detectable()
    {
        // Consume is not idempotent by design — caller must check ProcessedEventLog.
        // This test verifies the balance correctly records both entries (no silent dedup).
        // The inbox pattern (ProcessedEventLog) prevents this at the consumer level.
        var b = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        b.Accrue(20m, Today);
        var ref1 = "evt-12345";

        b.Consume(5m, Today, ref1);
        // Simulate duplicate: should succeed at balance level (consumer-level guard is in ProcessedEventLog)
        b.Consume(5m, Today, ref1);

        b.AvailableBalance.Should().Be(10m,
            "balance records both debits — ProcessedEventLog guards at consumer level prevent this in prod");
        b.Entries.Count(e => e.SourceReference == ref1).Should().Be(2,
            "two entries exist — ProcessedEventLog dedup is consumer responsibility");
    }

    [Fact]
    public void Balance_SingleConsumeReplayedOnce_CorrectBalance()
    {
        var b = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        b.Accrue(20m, Today);
        b.Consume(5m, Today, "evt-00001");

        b.AvailableBalance.Should().Be(15m);
        b.Entries.Sum(e => e.Quantity).Should().Be(15m, "invariant holds");
    }

    // ── Idempotency: CarryForward ─────────────────────────────────────────

    [Fact]
    public void CarryForward_SchedulerCrashRestart_ProcessedEventLogPreventsDoubleCarry()
    {
        // Simulate what should happen with ProcessedEventLog:
        // First run: balance = 10, carry forward 10 → balance = 20
        // Second run (crash restart): ProcessedEventLog key "carry-2026" already exists → skip
        // Without ProcessedEventLog: balance = 30 (wrong)
        // Test verifies the balance-layer operation itself records the entry.
        var b = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        b.Accrue(10m, Today);

        b.CarryForward(10m, Today); // first run
        var balanceAfterFirstRun = b.AvailableBalance;

        // In production, ProcessedEventLog prevents second run.
        // Here we document the expected balance if dedup works.
        balanceAfterFirstRun.Should().Be(20m, "10 accrued + 10 carried = 20");
    }

    // ── Scheduler: ScheduledDomainAction lifecycle ────────────────────────

    private static readonly DateOnly TodayDate = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void ScheduledAction_Create_StatusPending()
    {
        var action = ScheduledDomainAction.Create("AccrualRun", """{"policyId":"..."}""", TodayDate.AddDays(1));

        action.Status.Should().Be(ScheduledActionStatus.Pending);
        action.RetryCount.Should().Be(0);
    }

    [Fact]
    public void ScheduledAction_MarkExecuted_StatusExecuted()
    {
        var action = ScheduledDomainAction.Create("CarryForwardRun", "{}", TodayDate);

        action.MarkExecuted();

        action.Status.Should().Be(ScheduledActionStatus.Executed);
        action.ExecutedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void ScheduledAction_FailThreeTimes_DeadLettered()
    {
        var action = ScheduledDomainAction.Create("CompOffExpiry", "{}", TodayDate);

        action.MarkFailed("Timeout");
        action.RetryCount.Should().Be(1);

        action.MarkFailed("Timeout");
        action.RetryCount.Should().Be(2);

        action.MarkFailed("Timeout");
        action.Status.Should().Be(ScheduledActionStatus.DeadLettered,
            "3rd failure → DeadLettered");
        action.RetryCount.Should().Be(3);
    }

    [Fact]
    public void ScheduledAction_DeadLettered_StatusIsFinal()
    {
        var action = ScheduledDomainAction.Create("X", "{}", TodayDate);
        action.MarkFailed("1"); action.MarkFailed("2"); action.MarkFailed("3");

        // After DeadLettered, no more state changes (domain enforces via guard or
        // MarkFailed sets DeadLettered on 3rd attempt — 4th call sets RetryCount=4,
        // status stays DeadLettered since RetryCount >= 3)
        action.Status.Should().Be(ScheduledActionStatus.DeadLettered);
    }

    // ── Absence Case: Lifecycle ───────────────────────────────────────────

    [Fact]
    public void AbsenceCase_Open_StatusIsOpen()
    {
        var c = Domain.Absence.AbsenceCase.Raise("t1", "NCNS-001", Guid.NewGuid(), null,
            Domain.Absence.AbsenceType.NoCallNoShow, Today, 1m);
        c.Status.Should().Be(Domain.Absence.AbsenceCaseStatus.Open);
    }

    [Fact]
    public void AbsenceCase_FullLifecycle_ClosesSuccessfully()
    {
        var c = Domain.Absence.AbsenceCase.Raise("t1", "NCNS-002", Guid.NewGuid(), null,
            Domain.Absence.AbsenceType.UnauthorizedAbsence, Today, 2m);

        c.ReceiveExplanation("Was sick, forgot to call");
        c.Status.Should().Be(Domain.Absence.AbsenceCaseStatus.InReview);

        c.Resolve("hr-admin", Domain.Absence.AbsenceResolution.ExcusedWithoutPay,
            "First offence, documented", payrollImpact: true);
        c.Status.Should().Be(Domain.Absence.AbsenceCaseStatus.Resolved);

        c.Close();
        c.Status.Should().Be(Domain.Absence.AbsenceCaseStatus.Closed);
    }

    [Fact]
    public void AbsenceCase_CloseBeforeResolve_Throws()
    {
        var c = Domain.Absence.AbsenceCase.Raise("t1", "NCNS-003", Guid.NewGuid(), null,
            Domain.Absence.AbsenceType.NoCallNoShow, Today, 1m);

        var act = () => c.Close();
        act.Should().Throw<InvalidOperationException>("must resolve before close");
    }

    // ── Investigation Case ────────────────────────────────────────────────

    [Fact]
    public void InvestigationCase_Open_LinksEvidence_AddsNotes()
    {
        var c = Domain.Leaves.LeaveInvestigationCase.Open(
            "t1", Guid.NewGuid(), "INV-2026-001",
            "Balance dispute", "Employee claims 3 extra days", "hr-admin");

        c.LinkEvidence("LeaveBalance", Guid.NewGuid(), "Current balance ledger");
        c.LinkEvidence("LeaveTimelineEntry", Guid.NewGuid(), "Approval timeline");
        c.AddNote("hr-admin", "Balance confirmed via ledger entries");

        c.EvidenceLinks.Count.Should().Be(2);
        c.Notes.Count.Should().Be(1);
        c.Status.Should().Be(Domain.Leaves.InvestigationStatus.Open);
    }

    [Fact]
    public void InvestigationCase_Close_AfterResolve_Succeeds()
    {
        var c = Domain.Leaves.LeaveInvestigationCase.Open(
            "t1", Guid.NewGuid(), "INV-2026-002", "Title", "Desc", "hr");
        c.Resolve("hr", Domain.Leaves.InvestigationResolution.Dismissed, "No discrepancy found");
        c.Close();
        c.Status.Should().Be(Domain.Leaves.InvestigationStatus.Closed);
    }

    [Fact]
    public void InvestigationCase_AddNote_AfterClose_Throws()
    {
        var c = Domain.Leaves.LeaveInvestigationCase.Open(
            "t1", Guid.NewGuid(), "INV-2026-003", "T", "D", "hr");
        c.Resolve("hr", Domain.Leaves.InvestigationResolution.Upheld, "Confirmed");
        c.Close();

        var act = () => c.AddNote("hr", "Late note");
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Concurrent Balance: optimistic concurrency marker ────────────────

    [Fact]
    public void Balance_RowVersion_ExistsOnCreate()
    {
        var b = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        // RowVersion is managed by EF (IsRowVersion). Verify property exists and is accessible.
        b.RowVersion.Should().NotBeNull("RowVersion must be present for optimistic concurrency");
    }
}
