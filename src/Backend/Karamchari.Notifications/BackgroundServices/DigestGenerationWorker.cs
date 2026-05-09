using Karamchari.Notifications.Domain;
using Karamchari.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Notifications.BackgroundServices;

/// <summary>
/// Hosted service that triggers digest generation for all tenants on a schedule.
///
/// Schedule:
///   Daily digests:  runs every 15 minutes and generates for tenants whose
///                   preferred delivery time falls in the current window.
///   Weekly digests: runs on Monday between 07:00-08:00 UTC.
///
/// Phase 1: simplified â€” runs against all tenants with queued messages.
/// Phase 2: per-tenant delivery-time scheduling via TenantConfiguration.
///
/// Replay safety: DigestAggregatorService idempotency key blocks double-batch.
/// </summary>
public sealed class DigestGenerationWorker : BackgroundService
{
    private static readonly TimeSpan DailyCheckInterval = TimeSpan.FromMinutes(15);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DigestGenerationWorker> _logger;

    public DigestGenerationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<DigestGenerationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Stagger startup to avoid hammering DB on pod restart.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunDigestPassAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DigestGenerationWorker pass failed; will retry in {Interval}", DailyCheckInterval);
            }

            await Task.Delay(DailyCheckInterval, stoppingToken);
        }
    }

    private async Task RunDigestPassAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider
            .GetRequiredService<Karamchari.Notifications.Persistence.NotificationsDbContext>();
        var aggregator = scope.ServiceProvider
            .GetRequiredService<DigestAggregatorService>();

        // Discover all tenants with queued digest messages.
        var tenantIds = await db.NotificationMessages
            .Where(m => m.Status == NotificationStatus.DigestQueued)
            .Select(m => m.TenantId)
            .Distinct()
            .ToListAsync(ct);

        if (tenantIds.Count == 0) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isMonday = DateTime.UtcNow.DayOfWeek == DayOfWeek.Monday;
        var isWeeklyWindow = isMonday && DateTime.UtcNow.Hour is >= 7 and < 8;

        int totalBatches = 0;

        foreach (var tenantId in tenantIds)
        {
            int created = await aggregator.GenerateDailyDigestsAsync(tenantId, today, ct);
            totalBatches += created;

            if (isWeeklyWindow)
            {
                int weekly = await aggregator.GenerateWeeklyDigestsAsync(tenantId, today, ct);
                totalBatches += weekly;
            }
        }

        if (totalBatches > 0 && _logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "DigestGenerationWorker created {Count} batches across {TenantCount} tenants",
                totalBatches, tenantIds.Count);
    }
}
