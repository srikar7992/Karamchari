using System.Text.Json;
using Karamchari.PlatformIntelligence.Domain.Simulation;
using Karamchari.PlatformIntelligence.Persistence;
using Microsoft.Extensions.Logging;

namespace Karamchari.PlatformIntelligence.Services;

public sealed class WorkforceSimulationService
{
    private readonly PlatformIntelligenceDbContext _db;
    private readonly CrossDomainSignalAggregator _aggregator;
    private readonly ILogger<WorkforceSimulationService> _logger;

    public WorkforceSimulationService(
        PlatformIntelligenceDbContext db,
        CrossDomainSignalAggregator aggregator,
        ILogger<WorkforceSimulationService> logger)
    {
        _db = db;
        _aggregator = aggregator;
        _logger = logger;
    }

    private sealed record SimulationParameters(
        decimal AbsenteeismRate,
        int Retirements,
        int Resignations,
        decimal NewProjectDemandIncrease);

    public async Task<SimulationRun> RunAsync(
        string tenantId,
        string name,
        string parametersJson,
        string requestedBy,
        CancellationToken ct = default)
    {
        var run = SimulationRun.Create(tenantId, name, parametersJson, requestedBy);
        _db.SimulationRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        run.MarkRunning();
        await _db.SaveChangesAsync(ct);

        try
        {
            var parameters = JsonSerializer.Deserialize<SimulationParameters>(parametersJson)
                ?? throw new InvalidOperationException("Unable to deserialize simulation parameters");

            var signals = await _aggregator.AggregateAsync(tenantId, ct);

            var effectiveWorkforceReduction = parameters.AbsenteeismRate
                + (parameters.Retirements + parameters.Resignations) / 100.0m;
            var demandIncrease = parameters.NewProjectDemandIncrease;
            var netPressureIncrease = effectiveWorkforceReduction + demandIncrease;

            int? coverageCollapseInDays = netPressureIncrease switch
            {
                > 0.3m => 30,
                > 0.2m => 63,
                > 0.1m => 120,
                _ => null
            };

            var sitesAtRisk = signals.CriticalFragilitySites > 0
                ? new[] { "Multiple sites" }
                : Array.Empty<string>();

            var estimatedShortfall = (int)(netPressureIncrease * 50);

            var outcomeJson = JsonSerializer.Serialize(new
            {
                NetPressureIncrease = netPressureIncrease,
                EffectiveWorkforceReduction = effectiveWorkforceReduction,
                DemandIncrease = demandIncrease,
                CoverageCollapseInDays = coverageCollapseInDays,
                SitesAtRisk = sitesAtRisk,
                EstimatedShortfall = estimatedShortfall,
                BasedOnSignals = new
                {
                    signals.CoverageFragilityScore,
                    signals.BurnoutPressureScore,
                    signals.CriticalFragilitySites
                }
            });

            run.MarkCompleted(outcomeJson);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Simulation {RunId} completed for tenant {TenantId}: shortfall={Shortfall}, collapseInDays={CollapseInDays}",
                run.Id, tenantId, estimatedShortfall, coverageCollapseInDays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Simulation {RunId} failed for tenant {TenantId}", run.Id, tenantId);
            run.MarkFailed();
            await _db.SaveChangesAsync(ct);
        }

        return run;
    }
}
