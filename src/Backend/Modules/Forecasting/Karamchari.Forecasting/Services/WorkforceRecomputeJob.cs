// -----------------------------------------------------------------------
// <copyright file="WorkforceRecomputeJob.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Forecasting.Services;

/// <summary>
/// Nightly background job: triggers workforce planning recompute at 01:00 UTC.
/// After recompute, runs ForecastAccuracyCalculator to record yesterday's prediction errors.
/// </summary>
public sealed class WorkforceRecomputeJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkforceRecomputeJob> _logger;

    public WorkforceRecomputeJob(IServiceScopeFactory scopeFactory, ILogger<WorkforceRecomputeJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayUntilNextRun();
            _logger.LogInformation("WFP recompute job sleeping for {Minutes:F0} minutes until next run", delay.TotalMinutes);

            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            await RunRecomputeAsync(stoppingToken);
        }
    }

    private async Task RunRecomputeAsync(CancellationToken ct)
    {
        _logger.LogInformation("WFP nightly recompute starting at {Utc}", DateTimeOffset.UtcNow);

        try
        {
            // Phase 5.4: parallel per-tenant rebuild.
            // Each tenant gets its own DI scope (and therefore its own DbContext) to avoid
            // cross-thread EF Core access. MaxDegreeOfParallelism=4 caps DB connection pressure.

            List<string> tenantIds;
            await using (var listScope = _scopeFactory.CreateAsyncScope())
            {
                var svc = listScope.ServiceProvider.GetRequiredService<WorkforcePlanningService>();
                tenantIds = await svc.GetAllTenantIdsAsync(ct);
            }

            _logger.LogInformation(
                "WFP recompute: {Count} tenants queued for parallel rebuild", tenantIds.Count);

            await Parallel.ForEachAsync(tenantIds, new ParallelOptions
            {
                MaxDegreeOfParallelism = 4,
                CancellationToken = ct,
            }, async (tenantId, innerCt) =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<WorkforcePlanningService>();
                await service.RecomputeForTenantAsync(tenantId, innerCt);
            });

            // Accuracy calculation runs after all tenants are rebuilt
            await using (var accuracyScope = _scopeFactory.CreateAsyncScope())
            {
                var accuracyCalc = accuracyScope.ServiceProvider
                    .GetRequiredService<ForecastAccuracyCalculator>();
                await accuracyCalc.RecordYesterdayAccuracyAsync(ct);
            }

            _logger.LogInformation("WFP nightly recompute completed at {Utc}", DateTimeOffset.UtcNow);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "WFP nightly recompute failed");
        }
    }

    private static TimeSpan ComputeDelayUntilNextRun()
    {
        var now = DateTimeOffset.UtcNow;
        var nextRun = now.Date.AddHours(1); // 01:00 UTC
        if (nextRun <= now.DateTime)
            nextRun = nextRun.AddDays(1);
        return nextRun - now.DateTime;
    }
}
