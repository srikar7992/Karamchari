using Karamchari.Forecasting.Domain.Workforce;
using Karamchari.Forecasting.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Forecasting.Services;

/// <summary>
/// Runs what-if scenarios against the current demand/supply baseline.
///
/// For each active scenario in a tenant, applies parameter deltas to the baseline
/// and writes WfpScenarioResult rows showing the adjusted gap and risk level.
///
/// Scenario parameters:
/// - DemandMultiplier: scales demand (1.20 = +20% workload surge)
/// - AbsenteeismDeltaPercent: additional absence rate (5.0 = +5pp above baseline)
/// - AttritionDeltaPercent: additional quarterly attrition (2.0 = +2pp above baseline)
/// - AdditionalHireCount: extra headcount assumed hired (positive = more supply)
/// </summary>
public sealed class ScenarioEngine
{
    private static readonly ForecastHorizon[] AllHorizons =
    [
        ForecastHorizon.Days7,
        ForecastHorizon.Days14,
        ForecastHorizon.Days30,
        ForecastHorizon.Days90,
        ForecastHorizon.Days180,
        ForecastHorizon.Days365,
    ];

    private readonly ForecastingDbContext _db;
    private readonly ILogger<ScenarioEngine> _logger;

    public ScenarioEngine(ForecastingDbContext db, ILogger<ScenarioEngine> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RunAllScenariosForTenantAsync(string tenantId, CancellationToken ct)
    {
        var scenarios = await _db.WfpScenarioForecasts
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .ToListAsync(ct);

        foreach (var scenario in scenarios)
            await RunScenarioAsync(scenario, ct);
    }

    public async Task RunScenarioAsync(WfpScenarioForecast scenario, CancellationToken ct)
    {
        _logger.LogInformation("WFP scenario '{Name}' running for tenant {TenantId}", scenario.Name, scenario.TenantId);

        var projections = await _db.WorkforcePlanningProjections
            .Where(p => p.TenantId == scenario.TenantId)
            .ToListAsync(ct);

        // Remove prior results for this scenario
        var oldResults = await _db.WfpScenarioResults
            .Where(r => r.ScenarioId == scenario.Id)
            .ToListAsync(ct);
        _db.WfpScenarioResults.RemoveRange(oldResults);

        foreach (var proj in projections)
        {
            // Pull baseline demand from the most recent computed DemandForecast per horizon
            foreach (var horizon in AllHorizons)
            {
                var baseDemand = await _db.DemandForecasts
                    .Where(d => d.TenantId == scenario.TenantId &&
                                d.LocationCode == proj.LocationCode &&
                                d.SkillCode == proj.SkillCode &&
                                d.Horizon == horizon)
                    .OrderByDescending(d => d.GeneratedAt)
                    .FirstOrDefaultAsync(ct);

                var baseDemandCount = baseDemand?.RequiredHeadcount ?? Math.Max(1, proj.TotalEmployees);

                // Apply scenario demand multiplier
                var scenarioDemand = (int)Math.Ceiling(baseDemandCount * (double)scenario.DemandMultiplier);

                // Apply scenario supply deltas
                var horizonDays = (int)horizon;
                var adjustedAttritionRate = proj.HistoricalAttritionRatePercent + scenario.AttritionDeltaPercent;
                var adjustedAbsenceRate = proj.HistoricalAbsenteeismRate + scenario.AbsenteeismDeltaPercent;

                var extraAttrition = (int)Math.Round(
                    proj.TotalEmployees * (adjustedAttritionRate / 100m) * (horizonDays / 90.0m)) -
                    (int)Math.Round(
                    proj.TotalEmployees * (proj.HistoricalAttritionRatePercent / 100m) * (horizonDays / 90.0m));

                var extraAbsence = (int)Math.Round(proj.TotalEmployees * (scenario.AbsenteeismDeltaPercent / 100m));

                var baseSupply = await _db.SupplyForecasts
                    .Where(s => s.TenantId == scenario.TenantId &&
                                s.LocationCode == proj.LocationCode &&
                                s.SkillCode == proj.SkillCode &&
                                s.Horizon == horizon)
                    .OrderByDescending(s => s.GeneratedAt)
                    .FirstOrDefaultAsync(ct);

                var baseSupplyCount = baseSupply?.ExpectedHeadcount ?? proj.TotalEmployees;

                var scenarioSupply = Math.Max(0,
                    baseSupplyCount - extraAttrition - extraAbsence + scenario.AdditionalHireCount);

                var result = WfpScenarioResult.Compute(
                    scenario.Id,
                    scenario.TenantId,
                    proj.LocationCode,
                    proj.SkillCode,
                    horizon,
                    scenarioDemand,
                    scenarioSupply);

                _db.WfpScenarioResults.Add(result);
            }
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "WFP scenario '{Name}' complete: {Projections} projections × {Horizons} horizons",
            scenario.Name, projections.Count, AllHorizons.Length);
    }
}
