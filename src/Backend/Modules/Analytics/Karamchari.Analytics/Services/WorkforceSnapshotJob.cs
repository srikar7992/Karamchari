using Karamchari.Analytics.Domain;
using Karamchari.Analytics.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Analytics.Services;

public sealed class WorkforceSnapshotJob(
    IServiceScopeFactory scopeFactory,
    ILogger<WorkforceSnapshotJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var nextMidnight = DateTime.UtcNow.Date.AddDays(1);
            await Task.Delay(nextMidnight - DateTime.UtcNow, stoppingToken);

            try
            {
                using var scope = scopeFactory.CreateScope();
                await RunSnapshotAsync(scope, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "WorkforceSnapshotJob failed");
            }
        }
    }

    private async Task RunSnapshotAsync(IServiceScope scope, CancellationToken ct)
    {
        var analyticsDb = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dateKey = today.Year * 10000 + today.Month * 100 + today.Day;

        var activeEmployees = await analyticsDb.DimEmployees
            .AsNoTracking()
            .Where(e => e.IsActive)
            .Select(e => new { e.EmployeeId, e.TenantId, e.DepartmentId })
            .ToListAsync(ct);

        var rows = new List<FactWorkforceDaily>(activeEmployees.Count);
        foreach (var emp in activeEmployees)
        {
            var alreadyExists = await analyticsDb.FactWorkforceDaily
                .AnyAsync(f => f.TenantId == emp.TenantId && f.EmployeeId == emp.EmployeeId && f.DateKey == dateKey, ct);
            if (alreadyExists) continue;

            rows.Add(new FactWorkforceDaily
            {
                TenantId = emp.TenantId,
                DateKey = dateKey,
                EmployeeId = emp.EmployeeId,
                DepartmentId = emp.DepartmentId,
                IsActive = true
            });
        }

        analyticsDb.FactWorkforceDaily.AddRange(rows);
        await analyticsDb.SaveChangesAsync(ct);

        var byTenant = activeEmployees.GroupBy(e => e.TenantId);
        foreach (var group in byTenant)
        {
            await publisher.Publish(new Domain.Events.AnalyticsSnapshotCompleted(
                today, rows.Count(r => r.TenantId == group.Key), group.Key), ct);
        }

        logger.LogInformation("WorkforceSnapshotJob: {Rows} rows written for {Date}", rows.Count, today);

        await DetectDriftAsync(analyticsDb, rows, dateKey, ct);
    }

    private async Task DetectDriftAsync(
        AnalyticsDbContext db,
        List<FactWorkforceDaily> todayRows,
        int todayKey,
        CancellationToken ct)
    {
        // Yesterday's dateKey
        var d = new DateOnly(todayKey / 10000, (todayKey / 100) % 100, todayKey % 100);
        var yd = d.AddDays(-1);
        var yesterdayKey = yd.Year * 10000 + yd.Month * 100 + yd.Day;

        var yesterdayCounts = await db.FactWorkforceDaily
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(f => f.DateKey == yesterdayKey)
            .GroupBy(f => f.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);

        if (yesterdayCounts.Count == 0) return; // first run — no baseline yet

        var todayCounts = todayRows
            .GroupBy(r => r.TenantId)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var (tenant, todayCount) in todayCounts)
        {
            if (!yesterdayCounts.TryGetValue(tenant, out var yesterdayCount) || yesterdayCount == 0)
                continue;

            var drift = Math.Abs(todayCount - yesterdayCount) / (double)yesterdayCount;
            if (drift > 0.10)
            {
                logger.LogWarning(
                    "AnalyticsDrift tenant={TenantId} yesterday={Yesterday} today={Today} drift={DriftPct:P1} — projection may have diverged from operational data",
                    tenant, yesterdayCount, todayCount, drift);
            }
        }
    }
}
