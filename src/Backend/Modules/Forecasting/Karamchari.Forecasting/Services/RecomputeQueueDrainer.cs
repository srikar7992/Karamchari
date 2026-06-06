using Karamchari.Forecasting.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Forecasting.Services;

/// <summary>
/// Drains the WfpRecomputeEntry every 30 seconds.
///
/// Consumers enqueue dirty (tenant, location, skill) keys instead of calling RecomputeProjectionAsync
/// directly. The unique constraint on (TenantId, LocationCode, SkillCode) in WfpRecomputeEntry means
/// that 10,000 events for the same projection collapse into exactly 1 recompute — preventing the
/// fan-out explosion that would occur under high event volume.
///
/// Nightly full rebuild remains the authoritative fallback; this drainer keeps intra-day data fresh.
/// </summary>
public sealed class RecomputeQueueDrainer : BackgroundService
{
    private static readonly TimeSpan DrainInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecomputeQueueDrainer> _logger;

    public RecomputeQueueDrainer(IServiceScopeFactory scopeFactory, ILogger<RecomputeQueueDrainer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(DrainInterval, stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;

            await DrainAsync(stoppingToken);
        }
    }

    private async Task DrainAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ForecastingDbContext>();
            var wfpService = scope.ServiceProvider.GetRequiredService<WorkforcePlanningService>();

            var queued = await db.WfpRecomputeEntry
                .OrderBy(q => q.QueuedAt)
                .ToListAsync(ct);

            if (queued.Count == 0) return;

            _logger.LogDebug("WFP queue drainer: processing {Count} entries", queued.Count);

            // Remove queue entries before recompute so concurrent events can re-enqueue
            db.WfpRecomputeEntry.RemoveRange(queued);
            await db.SaveChangesAsync(ct);

            // Deduplicate in case unique constraint was not enforced on a prior entry
            var distinct = queued
                .Select(q => (q.TenantId, q.LocationCode, q.SkillCode))
                .Distinct()
                .ToList();

            foreach (var (tenantId, locationCode, skillCode) in distinct)
            {
                try
                {
                    await wfpService.RecomputeProjectionAsync(tenantId, locationCode, skillCode, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex,
                        "WFP queue drainer: recompute failed for {Tenant}/{Location}/{Skill}",
                        tenantId, locationCode, skillCode);
                }
            }

            _logger.LogDebug("WFP queue drainer: done — {Count} projections recomputed", distinct.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "WFP queue drainer cycle failed");
        }
    }
}
