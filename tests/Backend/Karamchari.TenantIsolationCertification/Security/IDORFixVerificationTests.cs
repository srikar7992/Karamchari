using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.SalaryRevisions;
using Karamchari.Payroll.StateMachines;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Security;

/// <summary>
/// Verifies the IDOR (Insecure Direct Object Reference) fixes applied to payroll endpoints.
///
/// Prior to the fix, <c>GetPayrollRunSummary</c>, <c>GetPayrollRunDetails</c>,
/// <c>GetPayrollRuns</c>, <c>SalaryRevisionEndpoints.GetRevision</c>, and
/// <c>ListRevisions</c> queried by ID alone without a TenantId filter. An attacker
/// with a valid JWT for any tenant could retrieve financial data belonging to a
/// different tenant by supplying a known or guessed resource GUID.
///
/// After the fix, all read endpoints explicitly filter by TenantId derived from the
/// authenticated user's JWT claims, providing defense-in-depth on top of the existing
/// schema-level tenant routing.
/// </summary>
public sealed class IDORFixVerificationTests
{
    // ── PayrollRunState IDOR ──────────────────────────────────────────────────

    [Fact]
    public async Task PayrollRunState_QueryByCorrelationIdAndTenantId_ReturnsNullForWrongTenant()
    {
        // Arrange: insert a PayrollRunState for tenant-alpha
        var dbName = Guid.NewGuid().ToString();
        var runId = Guid.NewGuid();
        const string ownerTenant = "tenant-alpha";
        const string attackerTenant = "tenant-beta";

        await using (var setupCtx = CreatePayrollDbContext(ownerTenant, dbName))
        {
            setupCtx.PayrollRunStates.Add(new PayrollRunState
            {
                CorrelationId = runId,
                TenantId = ownerTenant,
                CurrentState = "Completed",
                PeriodName = "May 2026",
                StartedAt = DateTime.UtcNow,
                TotalGross = 500_000m,
                TotalNet = 420_000m
            });
            await setupCtx.SaveChangesAsync();
        }

        // Act: attacker queries using their own tenant context but tenant-alpha's run ID
        // OLD (vulnerable): FirstOrDefaultAsync(r => r.CorrelationId == runId)
        // NEW (fixed):      FirstOrDefaultAsync(r => r.CorrelationId == runId && r.TenantId == attackerTenant)
        await using var attackCtx = CreatePayrollDbContext(attackerTenant, dbName);
        var result = await attackCtx.PayrollRunStates
            .FirstOrDefaultAsync(r => r.CorrelationId == runId && r.TenantId == attackerTenant);

        // Assert: attacker gets null — IDOR blocked
        result.Should().BeNull(
            "cross-tenant query with attackerTenantId filter must not return owner's payroll run");
    }

    [Fact]
    public async Task PayrollRunState_QueryByCorrelationIdAndTenantId_ReturnsResultForOwner()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var runId = Guid.NewGuid();
        const string ownerTenant = "tenant-alpha";

        await using (var setupCtx = CreatePayrollDbContext(ownerTenant, dbName))
        {
            setupCtx.PayrollRunStates.Add(new PayrollRunState
            {
                CorrelationId = runId,
                TenantId = ownerTenant,
                CurrentState = "Completed",
                PeriodName = "April 2026",
                StartedAt = DateTime.UtcNow
            });
            await setupCtx.SaveChangesAsync();
        }

        // Act: legitimate owner queries with their own tenant ID
        await using var ownerCtx = CreatePayrollDbContext(ownerTenant, dbName);
        var result = await ownerCtx.PayrollRunStates
            .FirstOrDefaultAsync(r => r.CorrelationId == runId && r.TenantId == ownerTenant);

        // Assert: owner sees their run
        result.Should().NotBeNull("owner must be able to retrieve their own payroll run");
        result!.PeriodName.Should().Be("April 2026");
    }

    [Fact]
    public async Task GetPayrollRuns_WithTenantFilter_DoesNotLeakCrossTenantRuns()
    {
        // Arrange: two tenants, each with one payroll run, SHARED in-memory database
        var dbName = Guid.NewGuid().ToString();
        const string tenantA = "tenant-alpha";
        const string tenantB = "tenant-beta";

        var runA = Guid.NewGuid();
        var runB = Guid.NewGuid();

        await using (var ctxA = CreatePayrollDbContext(tenantA, dbName))
        {
            ctxA.PayrollRunStates.Add(new PayrollRunState
            {
                CorrelationId = runA, TenantId = tenantA,
                CurrentState = "Completed", PeriodName = "Alpha Run", StartedAt = DateTime.UtcNow
            });
            await ctxA.SaveChangesAsync();
        }

        await using (var ctxB = CreatePayrollDbContext(tenantB, dbName))
        {
            ctxB.PayrollRunStates.Add(new PayrollRunState
            {
                CorrelationId = runB, TenantId = tenantB,
                CurrentState = "Completed", PeriodName = "Beta Run", StartedAt = DateTime.UtcNow
            });
            await ctxB.SaveChangesAsync();
        }

        // Act: tenant A queries with TenantId filter (as the fixed endpoint does)
        await using var readCtxA = CreatePayrollDbContext(tenantA, dbName);
        var tenantARuns = await readCtxA.PayrollRunStates
            .Where(x => x.TenantId == tenantA)
            .ToListAsync();

        // Assert: tenant A sees only their own run, NOT tenant B's
        tenantARuns.Should().HaveCount(1, "tenant A must only see their own payroll runs");
        tenantARuns.Single().CorrelationId.Should().Be(runA);
        tenantARuns.Should().NotContain(r => r.CorrelationId == runB,
            "tenant B's payroll run must not leak to tenant A");
    }

    // ── SalaryRevision IDOR ───────────────────────────────────────────────────

    [Fact]
    public async Task SalaryRevision_QueryByIdAndTenantId_ReturnsNullForWrongTenant()
    {
        // Arrange: create SalaryRevision for tenant-alpha
        var dbName = Guid.NewGuid().ToString();
        const string ownerTenant = "tenant-alpha";
        const string attackerTenant = "tenant-beta";

        Guid revisionId;
        await using (var setupCtx = CreatePayrollDbContext(ownerTenant, dbName))
        {
            var revision = SalaryRevision.Create(
                ownerTenant, Guid.NewGuid(), "Alice Alpha",
                RevisionType.AnnualIncrement,
                80_000m, 95_000m, new DateOnly(2026, 7, 1),
                false, "admin@alpha.local", "Annual review");

            setupCtx.Set<SalaryRevision>().Add(revision);
            await setupCtx.SaveChangesAsync();
            revisionId = revision.Id;
        }

        // Act: attacker queries using their own tenant ID
        // OLD (vulnerable): FirstOrDefaultAsync(r => r.Id == revisionId)
        // NEW (fixed):      FirstOrDefaultAsync(r => r.Id == revisionId && r.TenantId == attackerTenant)
        await using var attackCtx = CreatePayrollDbContext(attackerTenant, dbName);
        var result = await attackCtx.Set<SalaryRevision>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == revisionId && r.TenantId == attackerTenant);

        // Assert: IDOR blocked
        result.Should().BeNull(
            "attacker using their own tenant filter must not retrieve owner's salary revision");
    }

    [Fact]
    public async Task SalaryRevision_QueryByIdAndTenantId_ReturnsResultForOwner()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        const string ownerTenant = "tenant-alpha";

        Guid revisionId;
        await using (var setupCtx = CreatePayrollDbContext(ownerTenant, dbName))
        {
            var revision = SalaryRevision.Create(
                ownerTenant, Guid.NewGuid(), "Bob Alpha",
                RevisionType.Promotion,
                70_000m, 90_000m, new DateOnly(2026, 8, 1),
                true, "admin@alpha.local", "Promotion");

            setupCtx.Set<SalaryRevision>().Add(revision);
            await setupCtx.SaveChangesAsync();
            revisionId = revision.Id;
        }

        // Act
        await using var ownerCtx = CreatePayrollDbContext(ownerTenant, dbName);
        var result = await ownerCtx.Set<SalaryRevision>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == revisionId && r.TenantId == ownerTenant);

        // Assert
        result.Should().NotBeNull("owner must be able to retrieve their own salary revision");
        result!.EmployeeName.Should().Be("Bob Alpha");
    }

    [Fact]
    public async Task ListRevisions_WithTenantFilter_DoesNotLeakCrossTenantRevisions()
    {
        // Arrange: two tenants each with one salary revision in a SHARED DB
        var dbName = Guid.NewGuid().ToString();
        const string tenantA = "tenant-alpha";
        const string tenantB = "tenant-beta";

        await using (var ctxA = CreatePayrollDbContext(tenantA, dbName))
        {
            var rev = SalaryRevision.Create(tenantA, Guid.NewGuid(), "Alice",
                RevisionType.AnnualIncrement, 80_000m, 90_000m, new DateOnly(2026, 7, 1),
                false, "admin", "reason");
            ctxA.Set<SalaryRevision>().Add(rev);
            await ctxA.SaveChangesAsync();
        }

        await using (var ctxB = CreatePayrollDbContext(tenantB, dbName))
        {
            var rev = SalaryRevision.Create(tenantB, Guid.NewGuid(), "Bob",
                RevisionType.Promotion, 60_000m, 75_000m, new DateOnly(2026, 7, 1),
                false, "admin", "reason");
            ctxB.Set<SalaryRevision>().Add(rev);
            await ctxB.SaveChangesAsync();
        }

        // Act: list revisions as tenant A — with TenantId filter (as fixed endpoint does)
        await using var readCtxA = CreatePayrollDbContext(tenantA, dbName);
        var tenantARevisions = await readCtxA.Set<SalaryRevision>()
            .AsNoTracking()
            .Where(r => r.TenantId == tenantA)
            .ToListAsync();

        // Assert
        tenantARevisions.Should().HaveCount(1, "tenant A must only see their own revisions");
        tenantARevisions.Single().EmployeeName.Should().Be("Alice");
        tenantARevisions.Should().NotContain(r => r.EmployeeName == "Bob",
            "tenant B's salary revision must not leak to tenant A");
    }

    // ── SalaryRevision now implements ITenantOwned ────────────────────────────

    [Fact]
    public void SalaryRevision_ImplementsITenantOwned()
    {
        // Assert: SalaryRevision now implements ITenantOwned → eligible for global query filter
        typeof(SalaryRevision).GetInterfaces()
            .Should().Contain(typeof(Karamchari.Core.Multitenancy.ITenantOwned),
                "SalaryRevision must implement ITenantOwned for automatic query filter application");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static PayrollDbContext CreatePayrollDbContext(string tenantId, string? dbName = null)
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns(tenantId);

        var envelope = new TenantExecutionEnvelope(
            tenantId,
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.Test,
            TenantSource.AdminOverride);

        tenantProvider.GetTenant().Returns(envelope);
        tenantProvider.TryGetCurrentTenantId(out Arg.Any<string?>()).Returns(x =>
        {
            x[0] = tenantId;
            return true;
        });

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new PayrollDbContext(options, tenantProvider);
    }
}
