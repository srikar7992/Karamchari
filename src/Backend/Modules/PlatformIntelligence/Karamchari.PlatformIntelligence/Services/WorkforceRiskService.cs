using System.Text.Json;
using Karamchari.PlatformIntelligence.Domain.Platform;
using Karamchari.PlatformIntelligence.Domain.Risk;
using Karamchari.PlatformIntelligence.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.PlatformIntelligence.Services;

public sealed class WorkforceRiskService
{
    private readonly PlatformIntelligenceDbContext _db;
    private readonly CrossDomainSignalAggregator _aggregator;
    private readonly ILogger<WorkforceRiskService> _logger;

    public WorkforceRiskService(
        PlatformIntelligenceDbContext db,
        CrossDomainSignalAggregator aggregator,
        ILogger<WorkforceRiskService> logger)
    {
        _db = db;
        _aggregator = aggregator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WorkforceRisk>> EvaluateRisksAsync(
        string tenantId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Evaluating workforce risks for tenant {TenantId}", tenantId);

        var signals = await _aggregator.AggregateAsync(tenantId, ct);
        var evaluations = BuildEvaluations(tenantId, signals);

        // Idempotent: refresh existing or insert new per category
        foreach (var evaluation in evaluations)
        {
            var existing = await _db.WorkforceRisks
                .Where(r => r.TenantId == tenantId && r.Category == evaluation.Category)
                .FirstOrDefaultAsync(ct);

            if (existing is not null)
            {
                existing.Refresh(evaluation.Level, evaluation.RiskScore, evaluation.RationaleJson);
            }
            else
            {
                _db.WorkforceRisks.Add(evaluation);
            }
        }

        await _db.SaveChangesAsync(ct);

        return await _db.WorkforceRisks
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Category)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WorkforceRisk>> GetCurrentRisksAsync(
        string tenantId,
        CancellationToken ct = default)
        => await _db.WorkforceRisks
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Category)
            .ToListAsync(ct);

    private static List<WorkforceRisk> BuildEvaluations(string tenantId, PlatformSignalBundle signals)
    {
        return
        [
            BuildCoverageRisk(tenantId, signals),
            BuildBurnoutRisk(tenantId, signals),
            BuildAttritionRisk(tenantId, signals),
            BuildComplianceRisk(tenantId, signals),
            BuildCostRisk(tenantId, signals)
        ];
    }

    private static WorkforceRisk BuildCoverageRisk(string tenantId, PlatformSignalBundle signals)
    {
        var score = signals.CoverageFragilityScore;
        var level = score switch
        {
            > 80 => RiskLevel.Critical,
            > 60 => RiskLevel.High,
            > 30 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
        var rationale = new
        {
            FragilityScore = score,
            CriticalSites = signals.CriticalFragilitySites,
            Note = $"{signals.CriticalFragilitySites} sites at critical fragility"
        };
        return WorkforceRisk.Create(tenantId, RiskCategory.Coverage, level, score,
            JsonSerializer.Serialize(rationale));
    }

    private static WorkforceRisk BuildBurnoutRisk(string tenantId, PlatformSignalBundle signals)
    {
        var score = signals.BurnoutPressureScore;
        var level = score switch
        {
            > 80 => RiskLevel.Critical,
            > 60 => RiskLevel.High,
            > 30 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
        var rationale = new
        {
            BurnoutPressure = score,
            Note = "Average burnout score across workforce hotspots"
        };
        return WorkforceRisk.Create(tenantId, RiskCategory.Burnout, level, score,
            JsonSerializer.Serialize(rationale));
    }

    private static WorkforceRisk BuildAttritionRisk(string tenantId, PlatformSignalBundle signals)
    {
        var score = signals.AttritionPressureScore;
        var level = score switch
        {
            > 80 => RiskLevel.Critical,
            > 60 => RiskLevel.High,
            > 30 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
        var rationale = new
        {
            AttritionPressure = score,
            CriticalTalentRisk = signals.TalentRiskCriticalCount,
            Note = $"{signals.TalentRiskCriticalCount} employees at critical talent risk"
        };
        return WorkforceRisk.Create(tenantId, RiskCategory.Attrition, level, score,
            JsonSerializer.Serialize(rationale));
    }

    private static WorkforceRisk BuildComplianceRisk(string tenantId, PlatformSignalBundle signals)
    {
        // Invert compliance score: 100 → 0 risk, 0 → 100 risk
        var score = 100m - signals.ComplianceScore;
        var level = signals.OpenCriticalViolations > 0 ? RiskLevel.Critical
            : score switch
            {
                > 50 => RiskLevel.High,
                > 25 => RiskLevel.Medium,
                _ => RiskLevel.Low
            };
        var rationale = new
        {
            ComplianceScore = signals.ComplianceScore,
            OpenCritical = signals.OpenCriticalViolations,
            OpenHigh = signals.OpenHighViolations,
            Note = $"{signals.OpenCriticalViolations} critical + {signals.OpenHighViolations} high violations open"
        };
        return WorkforceRisk.Create(tenantId, RiskCategory.Compliance, level, score,
            JsonSerializer.Serialize(rationale));
    }

    private static WorkforceRisk BuildCostRisk(string tenantId, PlatformSignalBundle signals)
    {
        // Cost risk proxy: critical fragility sites × 20 + critical violations × 10
        var score = Math.Min(100m, signals.CriticalFragilitySites * 20m + signals.OpenCriticalViolations * 10m);
        var level = score switch
        {
            > 80 => RiskLevel.Critical,
            > 50 => RiskLevel.High,
            > 20 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
        var rationale = new
        {
            CriticalFragilitySites = signals.CriticalFragilitySites,
            CriticalViolations = signals.OpenCriticalViolations,
            Note = "Proxy: fragility remediation cost + compliance penalty exposure"
        };
        return WorkforceRisk.Create(tenantId, RiskCategory.Cost, level, score,
            JsonSerializer.Serialize(rationale));
    }
}
