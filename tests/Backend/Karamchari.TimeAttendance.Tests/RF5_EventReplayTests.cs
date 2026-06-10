// -----------------------------------------------------------------------
// <copyright file="RF5_EventReplayTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves;
using Xunit;

/// <summary>
/// RF#5 — Event Replay: domain idempotency documentation.
/// Production message brokers replay events — system must handle without double-debit.
/// Guard: ProcessedEventLog (inbox) at consumer level. Domain records every call.
/// These tests document replay behavior so team knows inbox IS required.
/// </summary>
public sealed class RF5_EventReplayTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    // ── Domain is NOT idempotent — inbox is required ──────────────────────

    [Fact]
    public void LeaveBalance_ReplayedConsume_RecordsBothEntries_InboxRequired()
    {
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        balance.Accrue(20m, Today);
        const string eventRef = "LeaveApproved-evt-00001";

        // First delivery
        balance.Consume(5m, Today, eventRef);
        var afterFirst = balance.AvailableBalance;

        // Replay (broker re-delivers)
        balance.Consume(5m, Today, eventRef);
        var afterReplay = balance.AvailableBalance;

        afterFirst.Should().Be(15m, "first consume deducted 5");
        afterReplay.Should().Be(10m,
            "domain records replay — ProcessedEventLog at consumer prevents this in production");

        balance.Entries.Count(e => e.SourceReference == eventRef)
               .Should().Be(2, "two entries prove inbox dedup is mandatory");
    }

    [Fact]
    public void LeaveRequest_StateIsImmutable_AfterApproval_ReplayCannotModify()
    {
        // Replay of LeaveApproved on an already-approved request must be idempotent at request level.
        // FinalizeApproved throws if already in Approved state → state machine provides guard.
        var r = LeaveRequest.Create(Guid.NewGuid(), Guid.NewGuid(),
            Today, Today.AddDays(4), 5);
        r.Submit();
        r.StartApproval();
        r.FinalizeApproved();

        // Replay: attempt to approve again
        var act = () => r.FinalizeApproved();
        act.Should().Throw<InvalidOperationException>(
            "state machine blocks re-approval — request is already Approved (terminal for re-approve)");
        r.Status.Should().Be(LeaveRequestStatus.Approved, "status unchanged after replay");
    }

    [Fact]
    public void LeaveBalance_ReplayedAccrue_WithCorrelationId_CanBeDetected()
    {
        // CorrelationId can be used by consumer to detect replay before calling domain.
        // Domain stores correlationId on each entry — consumer queries for existing correlationId.
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        const string correlationId = "accrual-run-2026-01";

        balance.Accrue(18m, Today, correlationId: correlationId);

        // Simulate consumer check before second delivery
        bool alreadyProcessed = balance.Entries.Any(e => e.CorrelationId == correlationId);
        alreadyProcessed.Should().BeTrue("consumer can detect replay by correlationId before calling domain");
    }

    [Fact]
    public void LeaveBalance_ReplayedRestore_InboxPrevents_DoubleCredit()
    {
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        balance.Accrue(10m, Today);
        balance.Consume(5m, Today, "req-001");
        const string cancelRef = "LeaveCancelled-evt-00002";

        // First delivery
        balance.Restore(5m, Today, cancelRef);
        var afterFirst = balance.AvailableBalance;

        // Replay: without inbox, this credits +5 again
        balance.Restore(5m, Today, cancelRef);

        afterFirst.Should().Be(10m, "restore brought balance back to 10");
        balance.AvailableBalance.Should().Be(15m,
            "without inbox: replay adds 5 again. Balance = 15 (WRONG in prod). Inbox prevents this.");
        balance.Entries.Count(e => e.SourceReference == cancelRef)
               .Should().Be(2, "two restore entries → inbox is not optional");
    }

    // ── Idempotent operations (safe to replay without inbox) ──────────────

    [Fact]
    public void LeaveBalance_PauseAccrual_Idempotent()
    {
        // PauseAccrual is a setter — calling twice doesn't double-pause.
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        balance.Accrue(10m, Today);

        balance.PauseAccrual("Suspension");
        balance.PauseAccrual("Suspension"); // replay

        balance.AccrualPaused.Should().BeTrue("idempotent — still paused");
        balance.Accrue(5m, Today); // no-op because paused
        balance.AvailableBalance.Should().Be(10m, "accrual skipped, no double-pause side effect");
    }

    [Fact]
    public void LeaveStateMachine_CanTransition_QueryIsIdempotent()
    {
        // CanTransition is a pure query — safe to call multiple times.
        var result1 = LeaveStateMachine.CanTransition(LeaveRequestStatus.Draft, LeaveRequestStatus.Submitted);
        var result2 = LeaveStateMachine.CanTransition(LeaveRequestStatus.Draft, LeaveRequestStatus.Submitted);
        result1.Should().Be(result2).And.BeTrue();
    }

    // ── ProcessedEventLog dedup pattern documentation ─────────────────────

    [Fact]
    public void EventReplay_DeduplicationPattern_Documented()
    {
        // Pattern used in production:
        // 1. Consumer receives event with EventId
        // 2. Check ProcessedEventLog.Exists(EventId, ConsumerName)
        // 3. If exists → skip (idempotent)
        // 4. If not → process + insert ProcessedEventLog entry in same transaction
        //
        // This test documents the expected dedup key structure.
        var eventId = Guid.NewGuid();
        const string consumerName = "LeaveApprovedConsumer";

        // ProcessedEventLog key = (EventId, ConsumerName) — composite PK in DbContext
        var key = (EventId: eventId, ConsumerName: consumerName);
        key.EventId.Should().NotBe(Guid.Empty);
        key.ConsumerName.Should().NotBeEmpty();
        // Full dedup test requires real DB transaction semantics (Testcontainers).
    }
}
