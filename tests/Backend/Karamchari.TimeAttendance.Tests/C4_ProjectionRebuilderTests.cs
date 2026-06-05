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
/// Concern 4 — Projection Rebuilder: rebuild after deletion, corruption, idempotency.
/// Uses EF InMemory — sufficient for rebuilder logic correctness.
/// Transactional safety + concurrent rebuild requires Testcontainers SQL Server.
/// </summary>
public sealed class C4_ProjectionRebuilderTests
{
    private const string Tenant = "test-tenant";
    private static readonly DateOnly Year2026Start = new DateOnly(2026, 1, 1);
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static LeaveRequest Approved(Guid empId, DateOnly start, DateOnly end, double days)
    {
        var r = LeaveRequest.Create(empId, Guid.NewGuid(), start, end, days);
        r.Submit(); r.StartApproval(); r.FinalizeApproved();
        return r.WithTenant(Tenant);
    }

    private static LeaveYear Year2026() => LeaveYear.CalendarYear(2026).WithTenant(Tenant);

    // ── Scenario A: Rebuild after complete deletion ───────────────────────

    [Fact]
    public async Task RebuildCalendar_AfterCompleteDelete_RecreatesAllEntries()
    {
        using var db = TestDbContextFactory.Create(Tenant);
        var rebuilder = new LeaveProjectionRebuilder(db, NullLogger<LeaveProjectionRebuilder>.Instance);

        db.LeaveYears.Add(Year2026());
        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();
        db.LeaveRequests.AddRange(
            Approved(empA, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 5), 5),
            Approved(empA, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), 2),
            Approved(empB, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 3), 3));
        await db.SaveChangesAsync();

        // Simulate complete deletion
        db.LeaveCalendarEntries.RemoveRange(await db.LeaveCalendarEntries.ToListAsync());
        await db.SaveChangesAsync();

        await rebuilder.RebuildAsync(Tenant);

        var rebuilt = await db.LeaveCalendarEntries.Where(e => e.TenantId == Tenant).ToListAsync();
        rebuilt.Should().HaveCount(10, "5+2+3 days rebuilt");
        rebuilt.Count(e => e.EmployeeId == empA).Should().Be(7);
        rebuilt.Count(e => e.EmployeeId == empB).Should().Be(3);
        rebuilt.Should().AllSatisfy(e => e.Status.Should().Be(LeaveCalendarEntryStatus.Approved));
    }

    // ── Scenario B: Rebuild corrects corruption ───────────────────────────

    [Fact]
    public async Task RebuildCalendar_AfterPartialCorruption_CorrectsFully()
    {
        using var db = TestDbContextFactory.Create(Tenant);
        var rebuilder = new LeaveProjectionRebuilder(db, NullLogger<LeaveProjectionRebuilder>.Instance);

        db.LeaveYears.Add(Year2026());
        db.LeaveRequests.Add(Approved(Guid.NewGuid(), new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 5), 5));
        await db.SaveChangesAsync();

        // First rebuild
        await rebuilder.RebuildAsync(Tenant);
        (await db.LeaveCalendarEntries.CountAsync()).Should().Be(5);

        // Corrupt: add orphaned entry
        db.LeaveCalendarEntries.Add(new LeaveCalendarEntry
        {
            TenantId = Tenant,
            EmployeeId = Guid.NewGuid(),
            Date = new DateOnly(2026, 5, 1),
            LeaveRequestId = Guid.NewGuid(),
            LeaveTypeId = Guid.NewGuid(),
            Duration = 1m,
            Status = LeaveCalendarEntryStatus.Approved,
            LastUpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
        (await db.LeaveCalendarEntries.CountAsync()).Should().Be(6, "corrupt entry added");

        // Rebuild clears and recreates
        await rebuilder.RebuildAsync(Tenant);
        (await db.LeaveCalendarEntries.CountAsync()).Should().Be(5, "corrupt entry eliminated");
    }

    // ── Scenario C: Rebuild twice is idempotent ───────────────────────────

    [Fact]
    public async Task RebuildCalendar_Twice_IsIdempotent()
    {
        using var db = TestDbContextFactory.Create(Tenant);
        var rebuilder = new LeaveProjectionRebuilder(db, NullLogger<LeaveProjectionRebuilder>.Instance);

        db.LeaveYears.Add(Year2026());
        db.LeaveRequests.Add(Approved(Guid.NewGuid(), new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 3), 3));
        await db.SaveChangesAsync();

        await rebuilder.RebuildAsync(Tenant);
        var first = await db.LeaveCalendarEntries.CountAsync();

        await rebuilder.RebuildAsync(Tenant);
        var second = await db.LeaveCalendarEntries.CountAsync();

        first.Should().Be(3);
        second.Should().Be(3, "idempotent — no duplicates on second rebuild");
    }

    // ── Employee summary rebuild ──────────────────────────────────────────

    [Fact]
    public async Task RebuildSummary_CreatesCorrectDayTotals()
    {
        using var db = TestDbContextFactory.Create(Tenant);
        var rebuilder = new LeaveProjectionRebuilder(db, NullLogger<LeaveProjectionRebuilder>.Instance);

        db.LeaveYears.Add(Year2026());
        var emp = Guid.NewGuid();
        db.LeaveRequests.AddRange(
            Approved(emp, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 5), 5),
            Approved(emp, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 3), 3));
        await db.SaveChangesAsync();

        await rebuilder.RebuildAsync(Tenant);

        var summary = await db.EmployeeLeaveSummaries.FirstAsync(s => s.EmployeeId == emp);
        summary.TotalApprovedRequests.Should().Be(2);
        summary.TotalDaysOnLeaveYtd.Should().Be(8m, "5 + 3 = 8 days");
    }

    [Fact]
    public async Task RebuildSummary_Twice_IsIdempotent()
    {
        using var db = TestDbContextFactory.Create(Tenant);
        var rebuilder = new LeaveProjectionRebuilder(db, NullLogger<LeaveProjectionRebuilder>.Instance);

        db.LeaveYears.Add(Year2026());
        var emp = Guid.NewGuid();
        db.LeaveRequests.Add(Approved(emp, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 5), 5));
        await db.SaveChangesAsync();

        await rebuilder.RebuildAsync(Tenant);
        await rebuilder.RebuildAsync(Tenant);

        var summaries = await db.EmployeeLeaveSummaries.Where(s => s.EmployeeId == emp).ToListAsync();
        summaries.Should().HaveCount(1, "idempotent — no duplicate summary rows");
        summaries[0].TotalDaysOnLeaveYtd.Should().Be(5m);
    }

    [Fact]
    public async Task RebuildWithNoDraftRequests_ProducesNoCalendarEntries()
    {
        using var db = TestDbContextFactory.Create(Tenant);
        var rebuilder = new LeaveProjectionRebuilder(db, NullLogger<LeaveProjectionRebuilder>.Instance);

        db.LeaveYears.Add(Year2026());
        var draft = LeaveRequest.Create(Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 5), 5).WithTenant(Tenant);
        db.LeaveRequests.Add(draft); // Draft — not Approved
        await db.SaveChangesAsync();

        await rebuilder.RebuildAsync(Tenant);

        (await db.LeaveCalendarEntries.CountAsync()).Should().Be(0, "drafts don't create calendar entries");
    }

    [Fact]
    public async Task RebuildCalendar_HalfDayRequest_DurationIs0Point5()
    {
        using var db = TestDbContextFactory.Create(Tenant);
        var rebuilder = new LeaveProjectionRebuilder(db, NullLogger<LeaveProjectionRebuilder>.Instance);

        db.LeaveYears.Add(Year2026());
        var emp = Guid.NewGuid();
        var halfDay = LeaveRequest.CreateHalfDay(emp, Guid.NewGuid(),
            new DateOnly(2026, 5, 1), HalfDayPeriod.FirstHalf).WithTenant(Tenant);
        halfDay.Submit(); halfDay.StartApproval(); halfDay.FinalizeApproved();
        db.LeaveRequests.Add(halfDay);
        await db.SaveChangesAsync();

        await rebuilder.RebuildAsync(Tenant);

        var entry = await db.LeaveCalendarEntries.SingleAsync();
        entry.Duration.Should().Be(0.5m, "half-day leave creates 0.5 duration calendar entry");
    }
}
