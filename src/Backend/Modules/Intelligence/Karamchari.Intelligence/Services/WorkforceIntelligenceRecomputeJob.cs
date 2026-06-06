using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Nightly background service that recomputes workforce intelligence scores
/// for all tenants. Runs at 02:00 UTC (after WFP recompute at 01:00 UTC).
///
/// Per-employee recalculation target: &lt;500ms.
/// </summary>
public sealed class WorkforceIntelligenceRecomputeJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkforceIntelligenceRecomputeJob> _logger;

    public WorkforceIntelligenceRecomputeJob(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkforceIntelligenceRecomputeJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1).AddHours(2); // 02:00 UTC tomorrow
            if (now.Hour < 2) nextRun = now.Date.AddHours(2); // today if before 02:00

            var delay = nextRun - now;
            _logger.LogDebug("Workforce intelligence recompute scheduled in {Delay}", delay);

            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { break; }

            await RunAsync(stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntelligenceDbContext>();
        var signalService = scope.ServiceProvider.GetRequiredService<WorkforceSignalService>();

        var tenantIds = await db.WorkforceSignalRecords
            .Select(s => s.TenantId)
            .Distinct()
            .ToListAsync(ct);

        _logger.LogInformation("Workforce intelligence recompute starting for {Count} tenants", tenantIds.Count);

        foreach (var tenantId in tenantIds)
        {
            try
            {
                await signalService.RecalculateTenantAsync(tenantId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workforce intelligence recompute failed for tenant {TenantId}", tenantId);
            }
        }

        _logger.LogInformation("Workforce intelligence recompute complete");
    }
}
