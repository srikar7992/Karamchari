using Karamchari.Forecasting.Domain;
using Karamchari.Forecasting.Domain.Events;
using Karamchari.Forecasting.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Forecasting.Services;

public sealed class HeadcountBudgetReconciler(
    IServiceScopeFactory scopeFactory,
    ILogger<HeadcountBudgetReconciler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ForecastingDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                await ReconcileAsync(db, publisher, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "HeadcountBudgetReconciler iteration failed");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private static async Task ReconcileAsync(
        ForecastingDbContext db, IPublishEndpoint publisher, CancellationToken ct)
    {
        var activeScenarios = await db.ForecastScenarios
            .Where(s => s.Status == ScenarioStatus.Active)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var scenario in activeScenarios)
        {
            foreach (var plan in scenario.HeadcountPlans.Where(p => p.FiscalYear == now.Year))
            {
                // Actual headcount sourced from HR read-model in same DB cross-schema
                // Stubbed until IHRReadService cross-module contract is established
                var actualHc = plan.PlannedHeadcount;

                var variance = new HeadcountVariance
                {
                    TenantId = scenario.TenantId,
                    ScenarioId = scenario.Id,
                    DepartmentId = plan.DepartmentId,
                    FiscalYear = now.Year,
                    Month = now.Month,
                    PlannedHeadcount = plan.PlannedHeadcount,
                    ActualHeadcount = actualHc
                };
                db.HeadcountVariances.Add(variance);

                var absVariance = Math.Abs(variance.Variance);
                if (plan.PlannedHeadcount > 0
                    && (decimal)absVariance / plan.PlannedHeadcount > 0.10m)
                {
                    await publisher.Publish(new HeadcountVarianceAlert(
                        scenario.Id, scenario.TenantId, plan.DepartmentId, now.Month, variance.Variance), ct);
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
