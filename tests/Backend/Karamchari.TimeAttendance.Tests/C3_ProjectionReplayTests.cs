namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.TimeAttendance.Services;
using Karamchari.TimeAttendance.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Concern 3 — Projection Replay Idempotency.
/// Production brokers retry events. Projection handler must be idempotent:
/// processing LeaveApproved twice must produce the same projection as processing it once.
/// Tests use EF InMemory + LeaveProjectionRebuilder as a proxy for projection handlers.
/// </summary>
public sealed class C3_ProjectionReplayTests
{
    private const string Tenant = "test-tenant";
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    // ── Calendar projection replay idempotency ────────────────────────────

    [Fact]
    public async Task CalendarProjection_RebuildAfterReplay_SameResult()
    {
        // Simulates: LeaveApproved emitted, projection handler crashed after first entry,
        // broker retried, projection handler ran again.
        // Expected: calendar ends up with correct entries, not doubled.
        using var db = TestDbContextFactory.Create(Tenant);
        var rebuilder = new LeaveProjectionRebuilder(db, NullLogger<LeaveProjectionRebuilder>.Instance);

        db.LeaveYears.Add(LeaveYear.CalendarYear(2026).WithTenant(Tenant));
        var emp = Guid.NewGuid();
        var req = LeaveRequest.Create(emp, Guid.NewGuid(),
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5), 5).WithTenant(Tenant);
        req.Submit(); req.StartApproval(); req.FinalizeApproved();
        db.LeaveRequests.Add(req);
        await db.SaveChangesAsync();

        // Simulate handler running twice (broker replay)
        await rebuilder.RebuildAsync(Tenant);
        await rebuilder.RebuildAsync(Tenant); // replay

        var entries = await db.LeaveCalendarEntries.Where(e => e.TenantId == Tenant).ToListAsync();
        entries.Should().HaveCount(5, "replay must not double-insert calendar entries");
        entries.Count(e => e.EmployeeId == emp).Should().Be(5);
    }

    [Fact]
    public async Task CalendarProjection_10Replays_Produces_ExactlySameResult()
    {
        using var db = TestDbContextFactory.Create(Tenant);
        var rebuilder = new LeaveProjectionRebuilder(db, NullLogger<LeaveProjectionRebuilder>.Instance);

        db.LeaveYears.Add(LeaveYear.CalendarYear(2026).WithTenant(Tenant));
        var req = LeaveRequest.Create(Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2), 2).WithTenant(Tenant);
        req.Submit(); req.StartApproval(); req.FinalizeApproved();
        db.LeaveRequests.Add(req);
        await db.SaveChangesAsync();

        // 10 replays
        for (int i = 0; i < 10; i++)
            await rebuilder.RebuildAsync(Tenant);

        var count = await db.LeaveCalendarEntries.CountAsync();
        count.Should().Be(2, "10 replays produce same 2-entry result as 1 run");
    }

    // ── Employee summary replay idempotency ───────────────────────────────

    [Fact]
    public async Task SummaryProjection_Replay_DaysNotDoubled()
    {
        using var db = TestDbContextFactory.Create(Tenant);
        var rebuilder = new LeaveProjectionRebuilder(db, NullLogger<LeaveProjectionRebuilder>.Instance);

        db.LeaveYears.Add(LeaveYear.CalendarYear(2026).WithTenant(Tenant));
        var emp = Guid.NewGuid();
        var req = LeaveRequest.Create(emp, Guid.NewGuid(),
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), 5).WithTenant(Tenant);
        req.Submit(); req.StartApproval(); req.FinalizeApproved();
        db.LeaveRequests.Add(req);
        await db.SaveChangesAsync();

        await rebuilder.RebuildAsync(Tenant);
        await rebuilder.RebuildAsync(Tenant); // broker retry

        var summaries = await db.EmployeeLeaveSummaries.Where(s => s.EmployeeId == emp).ToListAsync();
        summaries.Should().HaveCount(1, "summary row not duplicated on replay");
        summaries[0].TotalDaysOnLeaveYtd.Should().Be(5m, "5 days not doubled to 10");
        summaries[0].TotalApprovedRequests.Should().Be(1, "1 request not doubled to 2");
    }

    // ── LeaveRequest state machine prevents re-approval ───────────────────

    [Fact]
    public void LeaveRequest_AlreadyApproved_ReplayApprovalEvent_ThrowsIdempotently()
    {
        // Production pattern: consumer receives LeaveApproved event, processes it,
        // then broker replays the event. Consumer calls FinalizeApproved() again.
        // State machine throws — consumer handles exception, marks processed, skips.
        var r = LeaveRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Today, Today.AddDays(4), 5);
        r.Submit(); r.StartApproval(); r.FinalizeApproved();

        var act = () => r.FinalizeApproved(); // replay
        act.Should().Throw<InvalidOperationException>("already approved — state machine prevents double-approval");
        r.Status.Should().Be(LeaveRequestStatus.Approved, "status unchanged after replay attempt");
    }

    // ── Balance consumer replay: same consume called twice ────────────────

    [Fact]
    public void BalanceConsumer_Replayed_WithoutInbox_CausesDoubleDebit()
    {
        // Documents: without ProcessedEventLog, replay causes double debit.
        // Inbox pattern (ProcessedEventLog) MUST be used.
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        balance.Accrue(20m, Today);
        const string eventCorrelationId = "LeaveApproved-evt-111";

        balance.Consume(5m, Today, "req-1", correlationId: eventCorrelationId);
        balance.Consume(5m, Today, "req-1", correlationId: eventCorrelationId); // replay

        balance.AvailableBalance.Should().Be(10m,
            "double debit occurred — inbox is mandatory to prevent this");
        balance.Entries.Count(e => e.CorrelationId == eventCorrelationId)
               .Should().Be(2, "two entries prove inbox is not optional");
    }

    [Fact]
    public void BalanceConsumer_WithInboxCheck_PreventsDoubleDebit()
    {
        // Documents correct pattern: consumer checks ProcessedEventLog before calling domain.
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid());
        balance.Accrue(20m, Today);
        const string eventCorrelationId = "LeaveApproved-evt-222";

        // First delivery: consume
        balance.Consume(5m, Today, "req-2", correlationId: eventCorrelationId);

        // Replay: consumer checks ProcessedEventLog → correlationId found → skip
        bool alreadyProcessed = balance.Entries.Any(e => e.CorrelationId == eventCorrelationId);
        if (!alreadyProcessed) balance.Consume(5m, Today, "req-2", correlationId: eventCorrelationId);

        balance.AvailableBalance.Should().Be(15m, "correct: deducted only once");
        balance.Entries.Count(e => e.CorrelationId == eventCorrelationId).Should().Be(1);
    }
}
