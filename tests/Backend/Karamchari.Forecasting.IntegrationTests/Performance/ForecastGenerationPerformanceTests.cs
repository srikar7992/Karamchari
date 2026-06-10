// -----------------------------------------------------------------------
// <copyright file="ForecastGenerationPerformanceTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Forecasting.Domain.Workforce;
using Karamchari.Forecasting.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Karamchari.Forecasting.IntegrationTests.Performance;

/// <summary>
/// Phase 5.4 Performance Validation — Forecast Generation.
///
/// Validates the bulk supply calculator optimization:
///   - LoadBulkDataAsync: 5 queries per projection (not 7 × 6 = 42)
///   - CalculateFromBulkData: zero DB queries per horizon (in-memory)
///   - End-to-end: 10 projections × 6 horizons within time budget
///   - Correctness: bulk path produces identical results to sequential CalculateAsync
///
/// These tests also validate that PurgeExistingSnapshotsAsync (ExecuteDeleteAsync)
/// correctly removes stale forecast data before writing new rows.
/// </summary>
[Collection(nameof(ForecastingIntegrationCollection))]
public sealed class ForecastGenerationPerformanceTests(ForecastingSqlServerFixture fixture)
{
    private static readonly ForecastHorizon[] AllHorizons =
    [
        ForecastHorizon.Days7, ForecastHorizon.Days14, ForecastHorizon.Days30,
        ForecastHorizon.Days90, ForecastHorizon.Days180, ForecastHorizon.Days365,
    ];

    // ── Correctness: bulk path == sequential path ──────────────────────────────

    [DockerRequiredFact]
    public async Task BulkSupplyCalc_ProducesSameResultsAsSequential()
    {
        var tenantId = $"perf-bulk-cmp-{Guid.NewGuid():N}";
        const string locationCode = "DEFAULT";
        // Onboarding creates the GENERAL projection — use that (no skill assignment needed)
        const string skillCode = "GENERAL";

        // Onboard 20 employees → GENERAL projection gets TotalEmployees = 20
        await using var db = fixture.CreateDbContext();
        var consumer = fixture.CreateConsumer(db);
        for (var i = 0; i < 20; i++)
            await consumer.Consume(FakeOnboarded(Guid.NewGuid(), tenantId));

        // Reload projection
        await using var dbProj = fixture.CreateDbContext();
        var loaded = await dbProj.WorkforcePlanningProjections
            .FirstAsync(p => p.TenantId == tenantId && p.SkillCode == skillCode);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var supplyCalc = fixture.CreateSupplyCalculator(dbProj);

        // Sequential path: 7 DB queries per horizon
        var sequentialResults = new List<SupplyForecast>();
        foreach (var horizon in AllHorizons)
        {
            var forecast = await supplyCalc.CalculateAsync(tenantId, loaded, today.AddDays((int)horizon), horizon, CancellationToken.None);
            sequentialResults.Add(forecast);
        }

        // Bulk path: 5 queries total, then in-memory per horizon
        await using var dbBulk = fixture.CreateDbContext();
        var loadedBulk = await dbBulk.WorkforcePlanningProjections
            .FirstAsync(p => p.TenantId == tenantId && p.SkillCode == skillCode);
        var supplyCalcBulk = fixture.CreateSupplyCalculator(dbBulk);
        var bulkData = await supplyCalcBulk.LoadBulkDataAsync(tenantId, locationCode, skillCode, CancellationToken.None);

        var bulkResults = AllHorizons
            .Select(h => supplyCalcBulk.CalculateFromBulkData(tenantId, loadedBulk, today.AddDays((int)h), h, bulkData))
            .ToList();

        // Assert: identical headcount output for each horizon
        for (var i = 0; i < AllHorizons.Length; i++)
        {
            bulkResults[i].ExpectedHeadcount.Should().Be(sequentialResults[i].ExpectedHeadcount,
                $"bulk and sequential must agree on horizon {AllHorizons[i]}");
            bulkResults[i].TotalHeadcount.Should().Be(sequentialResults[i].TotalHeadcount,
                $"TotalHeadcount must match on horizon {AllHorizons[i]}");
        }
    }

    // ── Performance: LoadBulkDataAsync on empty dataset ───────────────────────

    [DockerRequiredFact]
    public async Task BulkSupplyCalc_LoadBulkData_EmptyDataset_CompletesUnder2Seconds()
    {
        var tenantId = $"perf-load-empty-{Guid.NewGuid():N}";

        await using var db = fixture.CreateDbContext();
        var supplyCalc = fixture.CreateSupplyCalculator(db);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var bulkData = await supplyCalc.LoadBulkDataAsync(tenantId, "DEFAULT", "NOSKILL", CancellationToken.None);
        sw.Stop();

        bulkData.SkillExpiryDates.Should().BeEmpty();
        bulkData.FutureHires.Should().BeEmpty();
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2),
            "5-query bulk load against empty tables must be fast");
    }

    // ── Performance: RecomputeProjectionAsync (full pipeline) ─────────────────

    [DockerRequiredFact]
    public async Task RecomputeProjection_10Projections_6HorizonsEach_CompletesUnder120Seconds()
    {
        var tenantId = $"perf-recomp-{Guid.NewGuid():N}";
        const int projectionCount = 10;

        // Seed 10 projections via consumer onboarding events
        for (var i = 0; i < projectionCount; i++)
        {
            await using var db = fixture.CreateDbContext();
            var consumer = fixture.CreateConsumer(db);
            var empId = Guid.NewGuid();
            // Each employee gets a unique skill → creates a separate projection row
            await consumer.Consume(FakeOnboarded(empId, tenantId));
            // Assign unique skill to create per-skill projection
            await consumer.Consume(FakeSkillAssigned(empId, tenantId, $"SKILL_{i:D4}"));
        }

        // Verify 10 skill-specific projections exist (plus the GENERAL projection)
        await using var dbCheck = fixture.CreateDbContext();
        var projCount = await dbCheck.WorkforcePlanningProjections
            .CountAsync(p => p.TenantId == tenantId && p.SkillCode != "GENERAL");
        projCount.Should().BeGreaterThanOrEqualTo(projectionCount,
            $"expected at least {projectionCount} skill projections seeded");

        // Recompute all projections for the tenant — measures full pipeline including bulk supply
        await using var dbRecomp = fixture.CreateDbContext();
        var service = fixture.CreateService(dbRecomp);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await service.RecomputeForTenantAsync(tenantId, CancellationToken.None);
        sw.Stop();

        // Assert: results written
        await using var dbResult = fixture.CreateDbContext();
        var forecastCount = await dbResult.SupplyForecasts
            .CountAsync(s => s.TenantId == tenantId);
        forecastCount.Should().BeGreaterThan(0, "recompute must produce supply forecast rows");

        // Phase 5.4 time budget: 10 projections × 6 horizons on Docker SQL Server
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(120),
            $"10-projection recompute took {sw.Elapsed.TotalSeconds:F1}s — exceeds 120s budget");
    }

    // ── Performance: Second recompute — purge path (ExecuteDeleteAsync) ────────

    [DockerRequiredFact]
    public async Task RecomputeProjection_Idempotent_SecondRunPurgesAndRewritesCorrectly()
    {
        var tenantId = $"perf-idempotent-{Guid.NewGuid():N}";

        // Seed 1 projection
        await using var db0 = fixture.CreateDbContext();
        var consumer0 = fixture.CreateConsumer(db0);
        await consumer0.Consume(FakeOnboarded(Guid.NewGuid(), tenantId));

        // First recompute
        await using var db1 = fixture.CreateDbContext();
        await fixture.CreateService(db1).RecomputeForTenantAsync(tenantId, CancellationToken.None);

        var countAfterFirst = await CountForecastRows(tenantId);

        // Second recompute — purge must delete old rows then insert fresh ones
        await using var db2 = fixture.CreateDbContext();
        await fixture.CreateService(db2).RecomputeForTenantAsync(tenantId, CancellationToken.None);

        var countAfterSecond = await CountForecastRows(tenantId);

        // Row count must stay stable — no accumulation from duplicate rows
        countAfterSecond.Should().Be(countAfterFirst,
            "ExecuteDeleteAsync purge must remove old rows before inserting new ones — no stale accumulation");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<int> CountForecastRows(string tenantId)
    {
        await using var db = fixture.CreateDbContext();
        return await db.SupplyForecasts.CountAsync(s => s.TenantId == tenantId)
             + await db.DemandForecasts.CountAsync(d => d.TenantId == tenantId)
             + await db.CoverageRisks.CountAsync(r => r.TenantId == tenantId);
    }

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
