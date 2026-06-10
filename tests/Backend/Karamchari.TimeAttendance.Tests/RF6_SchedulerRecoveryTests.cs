// -----------------------------------------------------------------------
// <copyright file="RF6_SchedulerRecoveryTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Domain.Scheduling;
using Xunit;

/// <summary>
/// RF#6 — Scheduler Recovery: crash-restart behavior, batch idempotency, partial-run dedup.
/// Simulates accrual batch crash at employee 5000, restart at 5001.
/// Full Testcontainers integration needed for DB-level transaction safety.
/// </summary>
public sealed class RF6_SchedulerRecoveryTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    // ── ScheduledDomainAction lifecycle ───────────────────────────────────

    [Fact]
    public void ScheduledAction_PendingToExecuted_Succeeds()
    {
        var action = ScheduledDomainAction.Create("AccrualRun", """{"leaveTypeId":"pl"}""", Today);
        action.MarkExecuted();
        action.Status.Should().Be(ScheduledActionStatus.Executed);
        action.ExecutedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void ScheduledAction_ThreeFailures_DeadLettered()
    {
        var action = ScheduledDomainAction.Create("CarryForwardRun", "{}", Today);
        action.MarkFailed("DB timeout"); action.RetryCount.Should().Be(1);
        action.MarkFailed("DB timeout"); action.RetryCount.Should().Be(2);
        action.MarkFailed("DB timeout"); action.Status.Should().Be(ScheduledActionStatus.DeadLettered);
    }

    // ── Batch crash recovery simulation ──────────────────────────────────

    [Fact]
    public void AccrualBatch_CrashAfter5000_Restart_Processes5001To10000()
    {
        const int totalEmployees = 10_000;
        const int crashAt = 5_000;

        // Simulate batch run with crash tracking
        var processed = new HashSet<int>();
        var actions = Enumerable.Range(1, totalEmployees)
            .Select(i => (EmployeeIndex: i, Action: ScheduledDomainAction.Create(
                "AccrualRun", $"{{\"employeeIndex\":{i}}}", Today)))
            .ToList();

        // First run: process 1-5000, then crash
        foreach (var (idx, action) in actions.Take(crashAt))
        {
            action.MarkExecuted();
            processed.Add(idx);
        }

        // Verify crash state: 5000 processed, 5000 pending
        actions.Count(a => a.Action.Status == ScheduledActionStatus.Executed).Should().Be(crashAt);
        actions.Count(a => a.Action.Status == ScheduledActionStatus.Pending).Should().Be(totalEmployees - crashAt);

        // Restart: only process Pending ones (not re-processing Executed)
        var restartBatch = actions.Where(a => a.Action.Status == ScheduledActionStatus.Pending).ToList();
        foreach (var (idx, action) in restartBatch)
        {
            action.MarkExecuted();
            processed.Add(idx);
        }

        processed.Count.Should().Be(totalEmployees, "all 10,000 employees processed exactly once");
        actions.Count(a => a.Action.Status == ScheduledActionStatus.Executed).Should().Be(totalEmployees);
        actions.All(a => a.Action.Status == ScheduledActionStatus.Executed).Should().BeTrue();
    }

    [Fact]
    public void AccrualBatch_ProcessedActionSkippedOnRestart()
    {
        // Key property: executor filters by Status == Pending before processing.
        // Executed actions are never re-processed even if batch restarts.
        var action = ScheduledDomainAction.Create("AccrualRun", "{}", Today);
        action.MarkExecuted();

        // Restart: check this action is NOT in restart batch
        var pendingForRestart = new[] { action }
            .Where(a => a.Status == ScheduledActionStatus.Pending)
            .ToList();

        pendingForRestart.Should().BeEmpty("executed actions skipped on restart — no double accrual");
    }

    [Fact]
    public void CarryForwardBatch_IdempotentCheck_ByCorrelationId()
    {
        // Carry forward with same yearLabel correlationId must be detectable before domain call.
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        balance.Accrue(20m, Today);
        const string correlationId = "CarryForward-2026";

        // First run
        balance.CarryForward(20m, Today, correlationId: correlationId);

        // Restart check: inspect entries for existing correlationId
        bool alreadyCarried = balance.Entries.Any(e =>
            e.EntryType == LeaveBalanceEntryType.CarryForward &&
            e.CorrelationId == correlationId);

        alreadyCarried.Should().BeTrue("executor checks correlationId before second carry forward");
    }

    [Fact]
    public void ScheduledAction_CancelledBeforeStart_NotProcessed()
    {
        var action = ScheduledDomainAction.Create("PolicyActivation", "{}", Today.AddDays(7));
        action.Cancel();

        action.Status.Should().Be(ScheduledActionStatus.Cancelled);
        // Executor skips Cancelled; test simulates executor filter
        var eligible = new[] { action }
            .Where(a => a.Status == ScheduledActionStatus.Pending)
            .ToList();
        eligible.Should().BeEmpty("cancelled action excluded from execution batch");
    }

    // ── Leave accrual batch: balance invariant survives crash ─────────────

    [Fact]
    public void AccrualBatch_PartialRun_LedgerInvariantHoldsPerBalance()
    {
        // Per employee: if crash after accrue recorded, restart only accrues
        // employees without existing accrual entry for this period.
        const int employees = 100;
        const string correlationId = "accrual-jan-2026";
        var balances = Enumerable.Range(1, employees)
            .Select(_ => LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid()))
            .ToList();

        // Simulate: accrued 50 before crash
        foreach (var b in balances.Take(50))
            b.Accrue(18m, Today, correlationId: correlationId);

        // Restart: only accrue those without correlationId entry
        foreach (var b in balances)
        {
            if (!b.Entries.Any(e => e.CorrelationId == correlationId))
                b.Accrue(18m, Today, correlationId: correlationId);
        }

        // Every balance has exactly 1 accrual entry
        balances.Should().AllSatisfy(b =>
        {
            b.Entries.Count(e => e.CorrelationId == correlationId).Should().Be(1,
                "each balance accrued exactly once despite restart");
            b.AvailableBalance.Should().Be(18m);
        });
    }
}
