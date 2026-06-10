// -----------------------------------------------------------------------
// <copyright file="RecommendationService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Karamchari.PlatformIntelligence.Domain.Recommendations;
using Karamchari.PlatformIntelligence.Domain.Risk;
using Karamchari.PlatformIntelligence.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.PlatformIntelligence.Services;

public sealed class RecommendationService
{
    private readonly PlatformIntelligenceDbContext _db;
    private readonly WorkforceRiskService _riskService;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        PlatformIntelligenceDbContext db,
        WorkforceRiskService riskService,
        ILogger<RecommendationService> logger)
    {
        _db = db;
        _riskService = riskService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Recommendation>> GenerateRecommendationsAsync(
        string tenantId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating recommendations for tenant {TenantId}", tenantId);

        var risks = await _riskService.GetCurrentRisksAsync(tenantId, ct);
        if (risks.Count == 0)
            risks = await _riskService.EvaluateRisksAsync(tenantId, ct);

        var actionableRisks = risks.Where(r => r.Level >= RiskLevel.High).ToList();
        var generated = new List<Recommendation>();

        foreach (var risk in actionableRisks)
        {
            var rec = BuildRecommendation(tenantId, risk);
            _db.Recommendations.Add(rec);
            generated.Add(rec);
        }

        if (generated.Count > 0)
            await _db.SaveChangesAsync(ct);

        return generated;
    }

    public async Task<IReadOnlyList<Recommendation>> GetActiveRecommendationsAsync(
        string tenantId,
        CancellationToken ct = default)
        => await _db.Recommendations
            .Where(r => r.TenantId == tenantId && r.Status == RecommendationStatus.Active)
            .OrderByDescending(r => r.ConfidenceScore)
            .Take(20)
            .ToListAsync(ct);

    public async Task<Recommendation?> AcknowledgeAsync(Guid id, CancellationToken ct = default)
    {
        var rec = await _db.Recommendations.FindAsync(new object[] { id }, ct);
        if (rec is null) return null;
        rec.Acknowledge();
        await _db.SaveChangesAsync(ct);
        return rec;
    }

    public async Task<Recommendation?> DismissAsync(Guid id, CancellationToken ct = default)
    {
        var rec = await _db.Recommendations.FindAsync(new object[] { id }, ct);
        if (rec is null) return null;
        rec.Dismiss();
        await _db.SaveChangesAsync(ct);
        return rec;
    }

    private static Recommendation BuildRecommendation(string tenantId, WorkforceRisk risk)
    {
        var (title, actions, confidence, savings, riskReduction, benefit) = risk.Category switch
        {
            RiskCategory.Coverage => (
                "Resolve coverage fragility before it causes operational failure",
                new[] { "Cross-train employees from lower-risk sites", "Hire temporary staff for critical sites", "Redistribute shifts to reduce fragility concentration" },
                risk.Level == RiskLevel.Critical ? 91m : 78m,
                risk.Level == RiskLevel.Critical ? 420_000m : 180_000m,
                risk.Level == RiskLevel.Critical ? 42m : 25m,
                "Prevents coverage collapse and associated SLA penalties"
            ),
            RiskCategory.Burnout => (
                "Reduce burnout pressure to prevent attrition cascade",
                new[] { "Rotate employees from high-burnout sites", "Approve pending leave requests", "Adjust shift patterns to ensure adequate rest periods" },
                risk.Level == RiskLevel.Critical ? 85m : 72m,
                risk.Level == RiskLevel.Critical ? 350_000m : 150_000m,
                risk.Level == RiskLevel.Critical ? 38m : 22m,
                "Reduces attrition risk and improves productivity"
            ),
            RiskCategory.Attrition => (
                "Initiate retention interventions for high-risk employees",
                new[] { "Launch retention programs for critical-role employees", "Begin succession planning for at-risk positions", "Conduct stay interviews in high-attrition departments" },
                risk.Level == RiskLevel.Critical ? 88m : 70m,
                risk.Level == RiskLevel.Critical ? 580_000m : 240_000m,
                risk.Level == RiskLevel.Critical ? 45m : 28m,
                "Retains critical talent and avoids replacement cost"
            ),
            RiskCategory.Compliance => (
                "Resolve open compliance violations before audit exposure",
                new[] { "Close critical violations within 48 hours", "Schedule compliance review for high-violation sites", "Implement break and rest period tracking automation" },
                risk.Level == RiskLevel.Critical ? 95m : 82m,
                risk.Level == RiskLevel.Critical ? 200_000m : 80_000m,
                risk.Level == RiskLevel.Critical ? 60m : 35m,
                "Eliminates regulatory penalty exposure and audit risk"
            ),
            RiskCategory.Cost => (
                "Optimize allocation to reduce excess cost exposure",
                new[] { "Transfer employees from overstaffed to understaffed sites", "Review OT approvals at high-fragility sites", "Audit contract labor at highest-cost sites" },
                risk.Level == RiskLevel.Critical ? 82m : 68m,
                risk.Level == RiskLevel.Critical ? 480_000m : 200_000m,
                risk.Level == RiskLevel.Critical ? 35m : 20m,
                "Reduces operational cost without compromising coverage"
            ),
            _ => (
                "Review workforce risk",
                new[] { "Consult workforce analytics" },
                60m, 0m, 10m, "General risk mitigation"
            )
        };

        var actionJson = JsonSerializer.Serialize(actions);
        var benefitJson = JsonSerializer.Serialize(new
        {
            Summary = benefit,
            RiskReductionPercent = riskReduction,
            EstimatedAnnualSavings = savings,
            Confidence = confidence
        });

        return Recommendation.Create(
            tenantId,
            risk.Category.ToString(),
            title,
            actionJson,
            confidence,
            savings,
            riskReduction,
            benefitJson,
            risk.Id);
    }
}
