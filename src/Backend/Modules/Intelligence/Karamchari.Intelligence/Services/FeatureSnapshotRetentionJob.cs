using Karamchari.Intelligence.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Weekly background job that purges aged rows from append-only intelligence tables.
///
/// Default retention periods (configurable via Intelligence:RetentionDays:*):
///   Intel_FeatureSnapshots  — 730 days (2 years; sufficient for ML training datasets)
///   Intel_ScoreSnapshots    — 365 days (1 year; velocity windows are &lt;= 30 days)
///   Intel_ScoreAudits       — 365 days (1 year; audit trail for manager challenges)
///
/// Runs at 04:00 UTC on Sundays to avoid overlap with nightly recompute (02:00 UTC)
/// and feature snapshot job (03:00 UTC).
/// </summary>
public sealed class FeatureSnapshotRetentionJob : BackgroundService
{
    private const int DefaultFeatureRetentionDays = 730;
    private const int DefaultSnapshotRetentionDays = 365;
    private const int DefaultAuditRetentionDays = 365;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeatureSnapshotRetentionJob> _logger;

    public FeatureSnapshotRetentionJob(
        IServiceScopeFactory scopeFactory,
        ILogger<FeatureSnapshotRetentionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextSunday4am = NextSunday4amUtc(now);
            var delay = nextSunday4am - now;

            _logger.LogDebug("Retention job sleeping until {NextRun:O}", nextSunday4am);
            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            await RunRetentionPassAsync(stoppingToken);
        }
    }

    private async Task RunRetentionPassAsync(CancellationToken ct)
    {
        _logger.LogInformation("Workforce intelligence retention pass started");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IntelligenceDbContext>();

        var featureCutoff = DateTime.UtcNow.AddDays(-DefaultFeatureRetentionDays);
        var snapshotCutoff = DateTime.UtcNow.AddDays(-DefaultSnapshotRetentionDays);
        var auditCutoff = DateTime.UtcNow.AddDays(-DefaultAuditRetentionDays);

        var deletedFeatures = await db.WorkforceFeatureSnapshots
            .Where(s => s.CapturedAt < featureCutoff)
            .ExecuteDeleteAsync(ct);

        var deletedSnapshots = await db.WorkforceScoreSnapshots
            .Where(s => s.CalculatedAt < snapshotCutoff)
            .ExecuteDeleteAsync(ct);

        var deletedAudits = await db.ScoreCalculationAudits
            .Where(a => a.CalculatedAt < auditCutoff)
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation(
            "Retention pass complete: features={F}, snapshots={S}, audits={A}",
            deletedFeatures, deletedSnapshots, deletedAudits);
    }

    private static DateTime NextSunday4amUtc(DateTime from)
    {
        // Find the next Sunday at 04:00 UTC
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)from.DayOfWeek + 7) % 7;
        if (daysUntilSunday == 0 && from.TimeOfDay >= TimeSpan.FromHours(4))
            daysUntilSunday = 7; // already past 04:00 today — next week

        var nextSunday = from.Date.AddDays(daysUntilSunday).AddHours(4);
        return DateTime.SpecifyKind(nextSunday, DateTimeKind.Utc);
    }
}
