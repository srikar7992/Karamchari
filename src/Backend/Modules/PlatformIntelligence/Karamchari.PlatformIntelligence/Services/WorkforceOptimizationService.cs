using System.Text.Json;
using Karamchari.Intelligence.Persistence;
using Karamchari.PlatformIntelligence.Domain.Optimization;
using Karamchari.PlatformIntelligence.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.PlatformIntelligence.Services;

public sealed class WorkforceOptimizationService
{
    private readonly PlatformIntelligenceDbContext _db;
    private readonly CrossDomainSignalAggregator _aggregator;
    private readonly IntelligenceDbContext _intelligence;
    private readonly ILogger<WorkforceOptimizationService> _logger;

    public WorkforceOptimizationService(
        PlatformIntelligenceDbContext db,
        CrossDomainSignalAggregator aggregator,
        IntelligenceDbContext intelligence,
        ILogger<WorkforceOptimizationService> logger)
    {
        _db = db;
        _aggregator = aggregator;
        _intelligence = intelligence;
        _logger = logger;
    }

    public async Task<WorkforceOptimization> OptimizeAsync(
        string tenantId,
        string objectiveType,
        string constraintsJson,
        CancellationToken ct = default)
    {
        var scopeKey = $"tenant:{tenantId}:objective:{objectiveType}";
        var optimization = WorkforceOptimization.Create(tenantId, scopeKey, objectiveType, constraintsJson);
        _db.WorkforceOptimizations.Add(optimization);
        await _db.SaveChangesAsync(ct);

        try
        {
            var signals = await _aggregator.AggregateAsync(tenantId, ct);

            var fragilities = await _intelligence.SiteCoverageFragilities
                .Where(x => x.TenantId == tenantId)
                .Select(x => new { x.FragilityScore })
                .ToListAsync(ct);

            var highRiskSites = fragilities.Where(x => x.FragilityScore > 70).ToList();
            var lowRiskSites = fragilities.Where(x => x.FragilityScore < 30).ToList();

            var recommendations = new List<string>();

            foreach (var site in highRiskSites)
                recommendations.Add($"High-risk site (fragility {site.FragilityScore:F1}) — needs reinforcement");

            foreach (var site in lowRiskSites)
                recommendations.Add($"Low-risk site (fragility {site.FragilityScore:F1}) — potential source for transfers");

            // Transfer recommendations for each high+low pair
            var pairCount = Math.Min(highRiskSites.Count, lowRiskSites.Count);
            for (int i = 0; i < pairCount; i++)
            {
                recommendations.Add(
                    $"Recommend transferring employees from low-risk site (fragility {lowRiskSites[i].FragilityScore:F1}) " +
                    $"to high-risk site (fragility {highRiskSites[i].FragilityScore:F1})");
            }

            // Objective-specific additions
            switch (objectiveType)
            {
                case "MinimizeCost":
                    recommendations.Add($"Reduce overtime by backfilling {highRiskSites.Count} high-risk sites — estimated OT reduction saves {highRiskSites.Count * 80_000m:C0} annually");
                    break;
                case "MaximizeCoverage":
                    recommendations.Add($"Priority fill {highRiskSites.Count} high-fragility sites to maximize coverage score");
                    break;
                case "BalanceWorkload":
                    recommendations.Add("Redistribute headcount to equalize fragility scores across all sites");
                    break;
            }

            var estimatedAnnualSavings = highRiskSites.Count * 200_000m;
            var recommendationsJson = JsonSerializer.Serialize(recommendations);

            optimization.MarkCompleted(recommendationsJson, estimatedAnnualSavings);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Optimization {OptimizationId} completed for tenant {TenantId}: savings={Savings:C0}",
                optimization.Id, tenantId, estimatedAnnualSavings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Optimization {OptimizationId} failed for tenant {TenantId}", optimization.Id, tenantId);
            optimization.MarkFailed();
            await _db.SaveChangesAsync(ct);
        }

        return optimization;
    }
}
