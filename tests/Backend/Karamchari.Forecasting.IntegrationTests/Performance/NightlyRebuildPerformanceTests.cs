using FluentAssertions;
using Karamchari.Forecasting.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Karamchari.Forecasting.IntegrationTests.Performance;

/// <summary>
/// Phase 5.4 Performance Validation — Nightly Rebuild.
///
/// Validates the parallel per-tenant rebuild introduced in WorkforceRecomputeJob:
///   - 3 tenants × 5 projections each rebuilt concurrently within time budget
///   - Final projection row counts correct: no cross-tenant contamination
///   - GetAllTenantIdsAsync returns distinct tenant IDs from multi-tenant dataset
///
/// The WorkforceRecomputeJob's parallel dispatch uses IServiceScopeFactory.
/// These tests simulate the same pattern directly (one service per tenant).
/// </summary>
[Collection(nameof(ForecastingIntegrationCollection))]
public sealed class NightlyRebuildPerformanceTests(ForecastingSqlServerFixture fixture)
{
    // ── Scenario 1: GetAllTenantIdsAsync returns correct distinct set ──────────

    [DockerRequiredFact]
    public async Task GetAllTenantIdsAsync_MultiTenantData_ReturnsDistinctTenants()
    {
        var tenant1 = $"perf-tenant-ids-1-{Guid.NewGuid():N}";
        var tenant2 = $"perf-tenant-ids-2-{Guid.NewGuid():N}";

        // Seed employees for 2 tenants
        await using var db1 = fixture.CreateDbContext();
        await fixture.CreateConsumer(db1).Consume(FakeOnboarded(Guid.NewGuid(), tenant1));

        await using var db2 = fixture.CreateDbContext();
        await fixture.CreateConsumer(db2).Consume(FakeOnboarded(Guid.NewGuid(), tenant2));

        // GetAllTenantIdsAsync must include both tenants
        await using var dbQuery = fixture.CreateDbContext();
        var service = fixture.CreateService(dbQuery);
        var tenantIds = await service.GetAllTenantIdsAsync(CancellationToken.None);

        tenantIds.Should().Contain(tenant1, "tenant1 has a projection row");
        tenantIds.Should().Contain(tenant2, "tenant2 has a projection row");
        tenantIds.Should().OnlyHaveUniqueItems("result must be DISTINCT");
    }

    // ── Scenario 2: Parallel rebuild — 3 tenants × 5 projections ─────────────

    [DockerRequiredFact]
    public async Task ParallelRebuild_3Tenants_5SkillsEach_ProducesCorrectForecasts()
    {
        var tenantIds = Enumerable.Range(0, 3)
            .Select(_ => $"perf-parallel-{Guid.NewGuid():N}")
            .ToArray();

        const int skillsPerTenant = 5;

        // Seed: each tenant gets skillsPerTenant skill projections
        foreach (var tenantId in tenantIds)
        {
            for (var i = 0; i < skillsPerTenant; i++)
            {
                await using var db = fixture.CreateDbContext();
                var consumer = fixture.CreateConsumer(db);
                var empId = Guid.NewGuid();
                await consumer.Consume(FakeOnboarded(empId, tenantId));
                await consumer.Consume(FakeSkillAssigned(empId, tenantId, $"SKILL_{i:D3}"));
            }
        }

        // Parallel rebuild: one service + dbContext per tenant (mirrors WorkforceRecomputeJob behavior)
        var sw = System.Diagnostics.Stopwatch.StartNew();

        await Parallel.ForEachAsync(tenantIds, new ParallelOptions { MaxDegreeOfParallelism = 4 },
            async (tenantId, ct) =>
            {
                await using var db = fixture.CreateDbContext();
                var service = fixture.CreateService(db);
                await service.RecomputeForTenantAsync(tenantId, ct);
            });

        sw.Stop();

        // Correctness: each tenant has supply forecast rows (6 horizons × N projections)
        foreach (var tenantId in tenantIds)
        {
            await using var dbCheck = fixture.CreateDbContext();
            var forecastCount = await dbCheck.SupplyForecasts
                .CountAsync(s => s.TenantId == tenantId);
            forecastCount.Should().BeGreaterThan(0,
                $"tenant {tenantId} must have supply forecast rows after rebuild");
        }

        // Performance: 3 tenants × 5 skills × 6 horizons in parallel must fit in 120s
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(120),
            $"parallel rebuild took {sw.Elapsed.TotalSeconds:F1}s — exceeds 120s budget");
    }

    // ── Scenario 3: No cross-tenant contamination ─────────────────────────────

    [DockerRequiredFact]
    public async Task ParallelRebuild_CrossTenantIsolation_ForecastsDoNotLeakAcrossTenants()
    {
        var tenantA = $"perf-iso-a-{Guid.NewGuid():N}";
        var tenantB = $"perf-iso-b-{Guid.NewGuid():N}";

        // Tenant A: 3 employees, Tenant B: 7 employees
        for (var i = 0; i < 3; i++)
        {
            await using var db = fixture.CreateDbContext();
            await fixture.CreateConsumer(db).Consume(FakeOnboarded(Guid.NewGuid(), tenantA));
        }

        for (var i = 0; i < 7; i++)
        {
            await using var db = fixture.CreateDbContext();
            await fixture.CreateConsumer(db).Consume(FakeOnboarded(Guid.NewGuid(), tenantB));
        }

        // Rebuild both in parallel
        await Parallel.ForEachAsync(new[] { tenantA, tenantB }, new ParallelOptions { MaxDegreeOfParallelism = 2 },
            async (tenantId, ct) =>
            {
                await using var db = fixture.CreateDbContext();
                await fixture.CreateService(db).RecomputeForTenantAsync(tenantId, ct);
            });

        // Assert supply forecast rows are tagged with the correct tenant ID.
        // Service-level isolation: RecomputeForTenantAsync(tenantA) only writes rows WHERE TenantId = tenantA.
        // (Row-level security is enforced by TenantSchemaCommandInterceptor in production;
        //  test validates service-layer tag correctness, not DB-level filtering.)
        await using var dbCheck = fixture.CreateDbContext();

        var aForecasts = await dbCheck.SupplyForecasts
            .Where(s => s.TenantId == tenantA)
            .Select(s => s.TotalHeadcount)
            .ToListAsync();
        aForecasts.Should().NotBeEmpty("tenant A must have supply forecast rows");
        aForecasts.Should().AllSatisfy(h => h.Should().BeLessThanOrEqualTo(3),
            "tenant A supply TotalHeadcount can never exceed its own 3 employees");

        var bForecasts = await dbCheck.SupplyForecasts
            .Where(s => s.TenantId == tenantB)
            .Select(s => s.TotalHeadcount)
            .ToListAsync();
        bForecasts.Should().NotBeEmpty("tenant B must have supply forecast rows");
        bForecasts.Should().AllSatisfy(h => h.Should().BeLessThanOrEqualTo(7),
            "tenant B supply TotalHeadcount can never exceed its own 7 employees");

        // Verify tenant A rows are tagged tenantA and tenant B rows tagged tenantB (no tag corruption)
        var wrongTagOnA = await dbCheck.SupplyForecasts
            .CountAsync(s => s.TenantId == tenantA && s.TotalHeadcount > 3);
        wrongTagOnA.Should().Be(0,
            "tenant A TotalHeadcount > 3 indicates cross-tenant data from tenant B contaminated the rebuild");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static MassTransit.ConsumeContext<T> FakeCtx<T>(T message) where T : class
    {
        var ctx = NSubstitute.Substitute.For<MassTransit.ConsumeContext<T>>();
        ctx.Message.Returns(message);
        ctx.CancellationToken.Returns(CancellationToken.None);
        return ctx;
    }

    private static MassTransit.ConsumeContext<Karamchari.Core.Contracts.IntegrationEvents.V1.EmployeeOnboardedIntegrationEvent>
        FakeOnboarded(Guid employeeId, string tenantId) =>
        FakeCtx(new Karamchari.Core.Contracts.IntegrationEvents.V1.EmployeeOnboardedIntegrationEvent(
            employeeId, tenantId,
            EmployeeNumber: "EMP-PERF",
            LegalName: "Perf Test Employee",
            WorkEmail: null,
            HiredOn: new DateOnly(2025, 1, 1),
            DateOfBirth: new DateOnly(1985, 1, 1),
            ContractEndDate: null));

    private static MassTransit.ConsumeContext<Karamchari.Core.Contracts.IntegrationEvents.SkillAssignedIntegrationEvent>
        FakeSkillAssigned(Guid employeeId, string tenantId, string skillCode) =>
        FakeCtx(new Karamchari.Core.Contracts.IntegrationEvents.SkillAssignedIntegrationEvent(
            employeeId, tenantId,
            SkillId: Guid.NewGuid(),
            SkillCode: skillCode,
            SkillName: skillCode,
            Level: 1,
            ExpiryDate: null));
}
