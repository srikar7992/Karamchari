using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Persistence;
using Karamchari.Intelligence.Services.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Computes and upserts <see cref="WorkforceHotspot"/> records for a tenant.
/// Called by <see cref="WorkforceIntelligenceRecomputeJob"/> after per-employee scoring completes.
/// </summary>
public sealed class HotspotDetectionService
{
    private readonly IntelligenceDbContext _db;
    private readonly ILogger<HotspotDetectionService> _logger;

    public HotspotDetectionService(IntelligenceDbContext db, ILogger<HotspotDetectionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Recomputes the tenant-wide hotspot aggregate and upserts the record.
    /// </summary>
    public async Task RecalculateTenantHotspotAsync(string tenantId, CancellationToken ct = default)
    {
        var pairs = await _db.WorkforceBurnoutScores
            .Where(b => b.TenantId == tenantId)
            .Join(
                _db.WorkforceAttritionScores.Where(a => a.TenantId == tenantId),
                b => b.EmployeeId,
                a => a.EmployeeId,
                (b, a) => new EmployeeScorePair(b.Score, a.Score))
            .ToListAsync(ct);

        if (pairs.Count == 0)
        {
            _logger.LogDebug("No score pairs for tenant {TenantId} — skipping hotspot", tenantId);
            return;
        }

        var result = HotspotCalculator.Calculate(pairs);
        var scopeKey = $"tenant:{tenantId}";

        var existing = await _db.WorkforceHotspots
            .FirstOrDefaultAsync(h => h.TenantId == tenantId && h.ScopeKey == scopeKey, ct);

        if (existing == null)
        {
            _db.WorkforceHotspots.Add(WorkforceHotspot.Create(
                tenantId, scopeKey,
                result.TotalEmployees,
                result.MeanBurnoutScore, result.MeanAttritionScore,
                result.BurnoutStdDev, result.AttritionStdDev,
                result.HighRiskBurnoutCount, result.CriticalRiskBurnoutCount,
                result.HighRiskAttritionCount, result.CriticalRiskAttritionCount));
        }
        else
        {
            existing.Refresh(
                result.TotalEmployees,
                result.MeanBurnoutScore, result.MeanAttritionScore,
                result.BurnoutStdDev, result.AttritionStdDev,
                result.HighRiskBurnoutCount, result.CriticalRiskBurnoutCount,
                result.HighRiskAttritionCount, result.CriticalRiskAttritionCount);
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Hotspot [{Scope}]: burnout={BurnoutMean:F1} ({BurnoutSev}), attrition={AttritionMean:F1} ({AttritionSev}), n={N}",
            scopeKey,
            result.MeanBurnoutScore, result.BurnoutSeverity,
            result.MeanAttritionScore, result.AttritionSeverity,
            result.TotalEmployees);
    }
}
