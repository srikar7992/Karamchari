// -----------------------------------------------------------------------
// <copyright file="CrossDomainSignalAggregator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compliance.Domain.Violations;
using Karamchari.Compliance.Persistence;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Persistence;
using Karamchari.PlatformIntelligence.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.PlatformIntelligence.Services;

public sealed class CrossDomainSignalAggregator
{
    private readonly IntelligenceDbContext _intelligence;
    private readonly ComplianceDbContext _compliance;
    private readonly ILogger<CrossDomainSignalAggregator> _logger;

    public CrossDomainSignalAggregator(
        IntelligenceDbContext intelligence,
        ComplianceDbContext compliance,
        ILogger<CrossDomainSignalAggregator> logger)
    {
        _intelligence = intelligence;
        _compliance = compliance;
        _logger = logger;
    }

    public async Task<PlatformSignalBundle> AggregateAsync(string tenantId, CancellationToken ct = default)
    {
        _logger.LogDebug("Aggregating cross-domain signals for tenant {TenantId}", tenantId);

        // Compliance signals
        var complianceScore = await _compliance.ComplianceScores
            .Where(x => x.TenantId == tenantId)
            .Select(x => (decimal?)x.Score)
            .FirstOrDefaultAsync(ct) ?? 100m;

        var openCriticalViolations = await _compliance.PolicyViolations
            .Where(x => x.TenantId == tenantId && x.Status == ViolationStatus.Open && x.Severity == ViolationSeverity.Critical)
            .CountAsync(ct);

        var openHighViolations = await _compliance.PolicyViolations
            .Where(x => x.TenantId == tenantId && x.Status == ViolationStatus.Open && x.Severity == ViolationSeverity.High)
            .CountAsync(ct);

        // Coverage fragility signals
        var fragilities = await _intelligence.SiteCoverageFragilities
            .Where(x => x.TenantId == tenantId)
            .Select(x => new { x.FragilityScore, x.FragilityLevel })
            .ToListAsync(ct);

        var avgFragilityScore = fragilities.Count > 0
            ? fragilities.Average(x => x.FragilityScore)
            : 0m;

        var criticalFragilitySites = fragilities.Count(x => x.FragilityLevel == CoverageFragilityLevel.Critical);

        // Burnout / attrition signals
        var hotspots = await _intelligence.WorkforceHotspots
            .Where(x => x.TenantId == tenantId)
            .Select(x => new { x.MeanBurnoutScore, x.MeanAttritionScore })
            .ToListAsync(ct);

        var avgBurnout = hotspots.Count > 0 ? hotspots.Average(x => x.MeanBurnoutScore) : 0m;
        var avgAttrition = hotspots.Count > 0 ? hotspots.Average(x => x.MeanAttritionScore) : 0m;

        // Talent risk signals
        var criticalTalentRiskCount = await _intelligence.TalentRiskScores
            .Where(x => x.TenantId == tenantId && x.RiskLevel == WorkforceRiskLevel.Critical)
            .CountAsync(ct);

        // Confidence score
        bool hasCompliance = fragilities.Count > 0;
        bool hasWorkforce = hotspots.Count > 0;
        bool hasTalent = criticalTalentRiskCount >= 0; // always present

        int confidenceScore = (hasCompliance && hasWorkforce && hasTalent) ? 80
            : (hasCompliance || hasWorkforce) ? 60
            : 40;

        return new PlatformSignalBundle(
            TenantId: tenantId,
            ComplianceScore: complianceScore,
            OpenCriticalViolations: openCriticalViolations,
            OpenHighViolations: openHighViolations,
            CoverageFragilityScore: avgFragilityScore,
            CriticalFragilitySites: criticalFragilitySites,
            BurnoutPressureScore: avgBurnout,
            AttritionPressureScore: avgAttrition,
            TalentRiskCriticalCount: criticalTalentRiskCount,
            ConfidenceScore: confidenceScore
        );
    }
}
