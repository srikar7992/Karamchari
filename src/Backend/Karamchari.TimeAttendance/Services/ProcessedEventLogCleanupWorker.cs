using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Background service that prunes <c>Analytics_ProcessedEventLog</c> rows older than <see cref="RetentionDays"/>.
/// Runs daily at a configurable time (default: 02:00 UTC).
///
/// Without this, the log grows unbounded and becomes a query bottleneck.
/// The DB-level unique index on (EventId, ConsumerName) still enforces idempotency for the retention window.
/// </summary>
public sealed class ProcessedEventLogCleanupWorker : BackgroundService
{
    /// <summary>Retain event log entries for 30 days. Events replayed beyond this window will re-process (safe — additive guards handle it).</summary>
    private const int RetentionDays = 30;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProcessedEventLogCleanupWorker> _logger;

    public ProcessedEventLogCleanupWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ProcessedEventLogCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "ProcessedEventLog cleanup failed.");
            }

            // Wait until next 02:00 UTC
            var now = DateTimeOffset.UtcNow;
            var next = now.Date.AddDays(1).AddHours(2);
            var delay = next - now;
            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-RetentionDays);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TimeAttendanceDbContext>();

        var deleted = await db.ProcessedEventLogs
            .Where(l => l.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
            _logger.LogInformation("ProcessedEventLog cleanup: deleted {Count} entries older than {Cutoff}.",
                deleted, cutoff);
    }
}
