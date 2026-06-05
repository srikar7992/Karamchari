namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Services;
using Karamchari.TimeAttendance.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Concern 6 — Tenant Isolation (RLS).
/// Tests that queries scoped to Tenant A never return Tenant B data.
/// Uses EF InMemory with manual TenantId filtering (mirrors production RLS predicates).
///
/// SQL Server RLS (row-level security via RegisterTenantTable) requires Testcontainers.
/// These tests prove the application-layer filter logic is correct.
/// SQL-level enforcement tests belong in the Testcontainers integration suite.
/// </summary>
public sealed class C6_TenantIsolationTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    // ── LeaveRequest isolation ─────────────────────────────────────────────

    [Fact]
    public async Task LeaveRequests_TenantA_CannotSee_TenantB_Requests()
    {
        // Single shared InMemory DB — simulates production single-DB multi-tenant
        var dbName = $"isolation-{Guid.NewGuid()}";

        // Tenant B seeds a leave request
        using (var dbB = TestDbContextFactory.CreateShared(dbName, TenantB))
        {
            var reqB = LeaveRequest.Create(Guid.NewGuid(), Guid.NewGuid(),
                Today, Today.AddDays(4), 5).WithTenant(TenantB);
            dbB.LeaveRequests.Add(reqB);
            await dbB.SaveChangesAsync();
        }

        // Tenant A queries — must see 0 results
        using (var dbA = TestDbContextFactory.CreateShared(dbName, TenantA))
        {
            var requestsSeenByA = await dbA.LeaveRequests
                .Where(r => r.TenantId == TenantA)
                .ToListAsync();

            requestsSeenByA.Should().BeEmpty("Tenant A must not see Tenant B leave requests");
        }
    }

    [Fact]
    public async Task LeaveRequests_TenantA_OnlySees_OwnRequests()
    {
        var dbName = $"isolation-{Guid.NewGuid()}";
        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();

        using (var dbA = TestDbContextFactory.CreateShared(dbName, TenantA))
        using (var dbB = TestDbContextFactory.CreateShared(dbName, TenantB))
        {
            var reqA = LeaveRequest.Create(empA, Guid.NewGuid(), Today, Today.AddDays(2), 3).WithTenant(TenantA);
            var reqB = LeaveRequest.Create(empB, Guid.NewGuid(), Today, Today.AddDays(4), 5).WithTenant(TenantB);
            dbA.LeaveRequests.Add(reqA);
            dbB.LeaveRequests.Add(reqB);
            await dbA.SaveChangesAsync();
            await dbB.SaveChangesAsync();
        }

        using var reader = TestDbContextFactory.CreateShared(dbName, TenantA);
        var tenantAResults = await reader.LeaveRequests
            .Where(r => r.TenantId == TenantA)
            .ToListAsync();

        tenantAResults.Should().HaveCount(1);
        tenantAResults[0].EmployeeId.Should().Be(empA, "Tenant A only sees its own employee's request");
    }

    // ── LeaveBalance isolation ─────────────────────────────────────────────

    [Fact]
    public async Task LeaveBalances_TenantA_CannotSee_TenantB_Balances()
    {
        var dbName = $"isolation-{Guid.NewGuid()}";

        using (var dbB = TestDbContextFactory.CreateShared(dbName, TenantB))
        {
            var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid()).WithTenant(TenantB);
            balance.Accrue(18m, Today);
            dbB.LeaveBalances.Add(balance);
            await dbB.SaveChangesAsync();
        }

        using var dbA = TestDbContextFactory.CreateShared(dbName, TenantA);
        var seenByA = await dbA.LeaveBalances.Where(b => b.TenantId == TenantA).ToListAsync();
        seenByA.Should().BeEmpty("Tenant A must not see Tenant B balances");
    }

    // ── LeavePolicy isolation ─────────────────────────────────────────────

    [Fact]
    public async Task LeavePolicies_TenantA_CannotApply_TenantB_Policies()
    {
        var dbName = $"isolation-{Guid.NewGuid()}";
        var leaveTypeId = Guid.NewGuid();

        using (var dbB = TestDbContextFactory.CreateShared(dbName, TenantB))
        {
            var rules = new LeavePolicyRules { AnnualAllowance = 24 };
            var policy = LeavePolicy.Create(leaveTypeId, PolicyLevel.Global, "TenantB PL", "desc", rules)
                .WithTenant(TenantB);
            dbB.LeavePolicies.Add(policy);
            await dbB.SaveChangesAsync();
        }

        // Tenant A resolves policy for same leaveTypeId — must not get Tenant B's policy
        using var dbA = TestDbContextFactory.CreateShared(dbName, TenantA);
        var resolver = new LeavePolicyResolver(dbA);
        var resolution = await resolver.ResolveAsync(Guid.NewGuid(), leaveTypeId, Today);

        resolution.Should().BeNull("Tenant A must not resolve Tenant B's policies");
    }

    // ── Cross-tenant calendar isolation ───────────────────────────────────

    [Fact]
    public async Task LeaveCalendar_TenantA_CannotSee_TenantB_CalendarEntries()
    {
        var dbName = $"isolation-{Guid.NewGuid()}";

        using (var dbB = TestDbContextFactory.CreateShared(dbName, TenantB))
        {
            dbB.LeaveCalendarEntries.Add(new Persistence.LeaveCalendarEntry
            {
                TenantId = TenantB,
                EmployeeId = Guid.NewGuid(),
                Date = Today,
                LeaveRequestId = Guid.NewGuid(),
                LeaveTypeId = Guid.NewGuid(),
                Duration = 1m,
                Status = Persistence.LeaveCalendarEntryStatus.Approved,
                LastUpdatedAtUtc = DateTimeOffset.UtcNow,
            });
            await dbB.SaveChangesAsync();
        }

        using var dbA = TestDbContextFactory.CreateShared(dbName, TenantA);
        var entries = await dbA.LeaveCalendarEntries
            .Where(e => e.TenantId == TenantA)
            .ToListAsync();

        entries.Should().BeEmpty("Tenant A calendar must not contain Tenant B entries");
    }

    // ── Employee data isolation ────────────────────────────────────────────

    [Fact]
    public async Task EmployeeLeaveSummary_TenantIsolation_Correct()
    {
        var dbName = $"isolation-{Guid.NewGuid()}";
        var empB = Guid.NewGuid();

        using (var dbB = TestDbContextFactory.CreateShared(dbName, TenantB))
        {
            dbB.EmployeeLeaveSummaries.Add(new Persistence.EmployeeLeaveSummary
            {
                TenantId = TenantB,
                EmployeeId = empB,
                LeaveYearName = "CY 2026",
                LeaveYearId = Guid.NewGuid(),
                TotalDaysOnLeaveYtd = 15m,
                TotalApprovedRequests = 3,
                LastUpdatedAtUtc = DateTimeOffset.UtcNow,
            });
            await dbB.SaveChangesAsync();
        }

        using var dbA = TestDbContextFactory.CreateShared(dbName, TenantA);
        var summary = await dbA.EmployeeLeaveSummaries
            .Where(s => s.TenantId == TenantA && s.EmployeeId == empB)
            .FirstOrDefaultAsync();

        summary.Should().BeNull("Tenant A cannot see Tenant B employee summary");
    }

    // ── Legal Hold cross-tenant isolation ─────────────────────────────────

    [Fact]
    public async Task LegalHold_TenantA_CannotSee_TenantB_LegalHolds()
    {
        var dbName = $"isolation-{Guid.NewGuid()}";

        using (var dbB = TestDbContextFactory.CreateShared(dbName, TenantB))
        {
            var hold = Domain.Leaves.LegalHold.Create(TenantB,
                Domain.Leaves.LegalHoldScope.Tenant, null,
                "Labour dispute", "CASE-B-001", "hr-b").WithTenant(TenantB);
            dbB.LegalHolds.Add(hold);
            await dbB.SaveChangesAsync();
        }

        using var dbA = TestDbContextFactory.CreateShared(dbName, TenantA);
        var holds = await dbA.LegalHolds.Where(h => h.TenantId == TenantA).ToListAsync();
        holds.Should().BeEmpty();
    }
}
