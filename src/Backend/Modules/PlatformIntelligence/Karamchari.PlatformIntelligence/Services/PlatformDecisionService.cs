using System.Text.Json;
using Karamchari.PlatformIntelligence.Domain.Decisions;
using Karamchari.PlatformIntelligence.Domain.Platform;
using Karamchari.PlatformIntelligence.Persistence;
using Microsoft.Extensions.Logging;

namespace Karamchari.PlatformIntelligence.Services;

public sealed class PlatformDecisionService
{
    private readonly PlatformIntelligenceDbContext _db;
    private readonly CrossDomainSignalAggregator _aggregator;
    private readonly ILogger<PlatformDecisionService> _logger;

    public PlatformDecisionService(
        PlatformIntelligenceDbContext db,
        CrossDomainSignalAggregator aggregator,
        ILogger<PlatformDecisionService> logger)
    {
        _db = db;
        _aggregator = aggregator;
        _logger = logger;
    }

    public async Task<PlatformDecision> GenerateDecisionAsync(
        string tenantId,
        string scopeKey,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating platform decision for tenant {TenantId}, scope {ScopeKey}", tenantId, scopeKey);

        var signals = await _aggregator.AggregateAsync(tenantId, ct);
        var (priority, title, recommendations, conflicts) = ResolveConflicts(signals);

        var recommendationsJson = JsonSerializer.Serialize(recommendations);
        var conflictsJson = JsonSerializer.Serialize(conflicts);

        var decision = PlatformDecision.Create(
            tenantId,
            scopeKey,
            priority,
            title,
            recommendationsJson,
            conflictsJson,
            signals.ConfidenceScore);

        _db.PlatformDecisions.Add(decision);
        await _db.SaveChangesAsync(ct);

        return decision;
    }

    private static (DecisionPriority priority, string title, List<string> recommendations, List<string> conflicts)
        ResolveConflicts(PlatformSignalBundle signals)
    {
        var recommendations = new List<string>();
        var conflicts = new List<string>();
        var priority = DecisionPriority.Medium;

        // Critical compliance + coverage + budget conflict
        if (signals.CoverageFragilityScore > 70 && signals.OpenHighViolations > 5 && signals.ComplianceScore < 70)
        {
            conflicts.Add("Coverage shortage + compliance violations + budget risk");
            recommendations.Add("Hire temporary staff rather than extending shifts — extending shifts will worsen violations");
            priority = DecisionPriority.Critical;
        }

        // Burnout + coverage conflict
        if (signals.BurnoutPressureScore > 70 && signals.CoverageFragilityScore > 60)
        {
            conflicts.Add("High burnout pressure combined with coverage fragility");
            recommendations.Add("Cross-train employees from lower-pressure sites before extending high-burnout teams");
            if (priority < DecisionPriority.High)
                priority = DecisionPriority.High;
        }

        // Attrition + critical talent conflict
        if (signals.AttritionPressureScore > 65 && signals.TalentRiskCriticalCount > 3)
        {
            conflicts.Add("Multiple critical-risk employees combined with high attrition pressure");
            recommendations.Add("Initiate retention programs and succession planning for critical roles immediately");
            if (priority < DecisionPriority.High)
                priority = DecisionPriority.High;
        }

        // Critical violations standalone recommendation
        if (signals.OpenCriticalViolations > 0)
        {
            recommendations.Add($"Resolve {signals.OpenCriticalViolations} critical compliance violations before scheduling changes");
        }

        var title = priority switch
        {
            DecisionPriority.Critical => "Critical workforce action required — multiple compounding risks detected",
            DecisionPriority.High => "High-priority workforce interventions needed",
            _ => "Workforce intelligence decision — standard review recommended"
        };

        return (priority, title, recommendations, conflicts);
    }
}
