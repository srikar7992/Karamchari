using System.Text.Json;
using Karamchari.PlatformIntelligence.Domain.Insights;
using Karamchari.PlatformIntelligence.Domain.Platform;
using Karamchari.PlatformIntelligence.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.PlatformIntelligence.Services;

public sealed class ExecutiveDigestService
{
    private readonly PlatformIntelligenceDbContext _db;
    private readonly CrossDomainSignalAggregator _aggregator;
    private readonly PlatformDecisionService _decisionService;
    private readonly ILogger<ExecutiveDigestService> _logger;

    public ExecutiveDigestService(
        PlatformIntelligenceDbContext db,
        CrossDomainSignalAggregator aggregator,
        PlatformDecisionService decisionService,
        ILogger<ExecutiveDigestService> logger)
    {
        _db = db;
        _aggregator = aggregator;
        _decisionService = decisionService;
        _logger = logger;
    }

    public async Task<ExecutiveDigest> GenerateDailyDigestAsync(string tenantId, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating daily executive digest for tenant {TenantId}", tenantId);

        var signals = await _aggregator.AggregateAsync(tenantId, ct);

        // Try to load today's decision; generate one if none exists
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existingDecision = await _db.PlatformDecisions
            .Where(x => x.TenantId == tenantId && DateOnly.FromDateTime(x.GeneratedAt) == today)
            .OrderByDescending(x => x.GeneratedAt)
            .FirstOrDefaultAsync(ct);

        var decision = existingDecision
            ?? await _decisionService.GenerateDecisionAsync(tenantId, $"daily:{today:yyyy-MM-dd}", ct);

        // Build headlines
        var headlines = BuildHeadlines(signals);
        var headlineJson = JsonSerializer.Serialize(headlines);

        // Metric summary
        var metricSummaryJson = JsonSerializer.Serialize(new
        {
            signals.ComplianceScore,
            signals.CoverageFragilityScore,
            signals.BurnoutPressureScore,
            signals.AttritionPressureScore,
            signals.TalentRiskCriticalCount
        });

        // Top risks
        var topRisks = BuildTopRisks(signals);
        var topRisksJson = JsonSerializer.Serialize(topRisks);

        // Top recommendations — take first 3 from the decision
        List<string>? allRecommendations = null;
        try
        {
            allRecommendations = JsonSerializer.Deserialize<List<string>>(decision.RecommendationsJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize decision recommendations");
        }

        var topRecommendations = allRecommendations?.Take(3).ToList() ?? new List<string>();
        var topRecommendationsJson = JsonSerializer.Serialize(topRecommendations);

        var digest = ExecutiveDigest.Create(
            tenantId,
            DigestPeriod.Daily,
            DateTime.UtcNow,
            headlineJson,
            metricSummaryJson,
            topRisksJson,
            topRecommendationsJson);

        _db.ExecutiveDigests.Add(digest);
        await _db.SaveChangesAsync(ct);

        return digest;
    }

    public async Task<ExecutiveDigest?> GetLatestDigestAsync(string tenantId, CancellationToken ct = default)
    {
        return await _db.ExecutiveDigests
            .Where(x => x.TenantId == tenantId && x.Period == DigestPeriod.Daily)
            .OrderByDescending(x => x.PeriodDate)
            .FirstOrDefaultAsync(ct);
    }

    private static List<string> BuildHeadlines(PlatformSignalBundle signals)
    {
        var headlines = new List<string>();

        if (signals.OpenCriticalViolations > 0)
            headlines.Add($"CRITICAL: {signals.OpenCriticalViolations} critical compliance violations require immediate action");

        if (signals.CoverageFragilityScore > 70)
            headlines.Add($"HIGH: Coverage fragility critical at {signals.CriticalFragilitySites} sites");

        if (signals.BurnoutPressureScore > 70)
            headlines.Add("HIGH: Burnout pressure elevated across workforce");

        if (signals.AttritionPressureScore > 65)
            headlines.Add("WARNING: Attrition risk elevated");

        if (headlines.Count == 0)
            headlines.Add("Workforce health within acceptable parameters");

        return headlines.Take(3).ToList();
    }

    private static List<string> BuildTopRisks(PlatformSignalBundle signals)
    {
        var risks = new List<(int Weight, string Description)>();

        if (signals.OpenCriticalViolations > 0)
            risks.Add((100, $"{signals.OpenCriticalViolations} open critical compliance violations"));

        if (signals.CoverageFragilityScore > 60)
            risks.Add((80, $"Coverage fragility score {signals.CoverageFragilityScore:F1} — {signals.CriticalFragilitySites} critical sites"));

        if (signals.BurnoutPressureScore > 60)
            risks.Add((70, $"Burnout pressure score {signals.BurnoutPressureScore:F1}"));

        if (signals.TalentRiskCriticalCount > 0)
            risks.Add((65, $"{signals.TalentRiskCriticalCount} employees at critical talent risk level"));

        if (signals.AttritionPressureScore > 55)
            risks.Add((60, $"Attrition pressure score {signals.AttritionPressureScore:F1}"));

        return risks
            .OrderByDescending(x => x.Weight)
            .Take(5)
            .Select(x => x.Description)
            .ToList();
    }
}
