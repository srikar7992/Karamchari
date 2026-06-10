// -----------------------------------------------------------------------
// <copyright file="RecomputeQueuePerformanceTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Forecasting.Domain.Workforce;
using Karamchari.Forecasting.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Karamchari.Forecasting.IntegrationTests.Performance;

/// <summary>
/// Phase 5.4 Performance Validation — Recompute Queue.
///
/// Validates three failure modes at scale:
///   1. Dedup: 200 identical enqueue calls → exactly 1 row (coalescing holds under load)
///   2. Throughput: 200 unique keys → all enqueued, completes within time budget
///   3. Concurrent race: 10 tasks racing on same key → unique constraint prevents doubles
///
/// Time budgets are conservative for Docker SQL Server on a CI runner.
/// </summary>
[Collection(nameof(ForecastingIntegrationCollection))]
public sealed class RecomputeQueuePerformanceTests(ForecastingSqlServerFixture fixture)
{
    // ── Scenario 1: High-volume dedup ─────────────────────────────────────────

    [DockerRequiredFact]
    public async Task QueueDedup_SameKey_200Times_ExactlyOneRowInserted()
    {
        var tenantId = $"perf-dedup-{Guid.NewGuid():N}";
        const string locationCode = "DEFAULT";
        const string skillCode = "WELDER";

        await using var db = fixture.CreateDbContext();
        var service = fixture.CreateService(db);

        // Enqueue 200 times — all should coalesce into 1 row
        for (var i = 0; i < 200; i++)
            await service.EnqueueRecomputeAsync(tenantId, locationCode, skillCode, CancellationToken.None);

        await using var dbCheck = fixture.CreateDbContext();
        var count = await dbCheck.WfpRecomputeEntry
            .CountAsync(q => q.TenantId == tenantId && q.SkillCode == skillCode);

        count.Should().Be(1, "200 enqueues for same key must coalesce to 1 queue entry");
    }

    // ── Scenario 2: Throughput — 200 unique keys ──────────────────────────────

    [DockerRequiredFact]
    public async Task QueueThroughput_200UniqueKeys_AllEnqueuedWithinTimeLimit()
    {
        var tenantId = $"perf-thru-{Guid.NewGuid():N}";
        const int keyCount = 200;

        var sw = System.Diagnostics.Stopwatch.StartNew();

        await using var db = fixture.CreateDbContext();
        var service = fixture.CreateService(db);

        for (var i = 0; i < keyCount; i++)
            await service.EnqueueRecomputeAsync(tenantId, "DEFAULT", $"SKILL_{i:D4}", CancellationToken.None);

        sw.Stop();

        // Correctness: all keys enqueued
        await using var dbCheck = fixture.CreateDbContext();
        var enqueued = await dbCheck.WfpRecomputeEntry
            .CountAsync(q => q.TenantId == tenantId);
        enqueued.Should().Be(keyCount, $"all {keyCount} unique keys must be enqueued");

        // Performance: 200 sequential enqueues must complete in < 30s on Docker SQL Server
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30),
            $"200 unique enqueues took {sw.Elapsed.TotalSeconds:F1}s — exceeds 30s budget");
    }

    // ── Scenario 3: Concurrent race — unique constraint as safety net ─────────

    [DockerRequiredFact]
    public async Task ConcurrentEnqueue_SameKey_10ParallelTasks_UniqueConstraintHolds()
    {
        var tenantId = $"perf-race-{Guid.NewGuid():N}";
        const string locationCode = "DEFAULT";
        const string skillCode = "CONCURRENT";

        // 10 parallel tasks, each with its own DbContext + Service
        // All race to enqueue the same key — only 1 should survive
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            await using var db = fixture.CreateDbContext();
            var service = fixture.CreateService(db);
            // Swallow exceptions: the duplicate key fix in EnqueueRecomputeAsync handles it.
            // If any task throws (unfixed race), this assertion will catch it below.
            try
            {
                await service.EnqueueRecomputeAsync(tenantId, locationCode, skillCode, CancellationToken.None);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"EnqueueRecomputeAsync must not throw on concurrent duplicate — got: {ex.GetType().Name}: {ex.Message}", ex);
            }
        });

        await Task.WhenAll(tasks);

        // Assert: unique constraint enforced — exactly 1 row despite 10 concurrent inserts
        await using var dbCheck = fixture.CreateDbContext();
        var count = await dbCheck.WfpRecomputeEntry
            .CountAsync(q => q.TenantId == tenantId && q.SkillCode == skillCode);

        count.Should().Be(1,
            "unique constraint on (TenantId, LocationCode, SkillCode) must coalesce concurrent inserts");
    }
}
