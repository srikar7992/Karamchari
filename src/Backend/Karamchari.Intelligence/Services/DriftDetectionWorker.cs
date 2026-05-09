using Karamchari.Intelligence.Domain.Signals;
using Karamchari.Intelligence.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Background worker to monitor the freshness of analytical projections and intelligence signals.
/// Ensures stale data is caught and alerted before executives make decisions on it.
/// </summary>
internal sealed class DriftDetectionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DriftDetectionWorker> _logger;
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromHours(24);

    public DriftDetectionWorker(IServiceProvider serviceProvider, ILogger<DriftDetectionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DetectDriftAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Intelligence Drift Detection scan.");
            }

            await Task.Delay(ScanInterval, stoppingToken);
        }
    }

    private async Task DetectDriftAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntelligenceDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var cutoff = DateTimeOffset.UtcNow.Subtract(StaleThreshold);

        // Scan for signals that are active but haven't been regenerated or refreshed recently
        var staleSignals = await db.IntelligenceSignals
            .AsNoTracking()
            .Where(s => s.ExpiresAtUtc == null || s.ExpiresAtUtc > DateTimeOffset.UtcNow)
            .Where(s => s.GeneratedAtUtc < cutoff)
            .Select(s => new { s.Id, s.TenantId, s.SignalType, s.SubjectId, s.GeneratedAtUtc })
            .Take(100) // Batching
            .ToListAsync(ct);

        foreach (var signal in staleSignals)
        {
            _logger.LogWarning("STALE_INTELLIGENCE: Signal {SignalId} of type {SignalType} for subject {SubjectId} was last generated at {GeneratedAt}. Exceeds freshness threshold.",
                signal.Id, signal.SignalType, signal.SubjectId, signal.GeneratedAtUtc);

            // Publish event so downstream dashboards can render a "Stale Data Warning" overlay
            await bus.Publish(new Karamchari.Intelligence.Contracts.StaleIntelligenceAlertEvent(
                signal.Id,
                signal.TenantId,
                signal.SignalType,
                signal.SubjectId,
                signal.GeneratedAtUtc,
                "Signal exceeds freshness SLA of 24 hours. Confidence degraded."
            ), ct);
        }
    }
}
