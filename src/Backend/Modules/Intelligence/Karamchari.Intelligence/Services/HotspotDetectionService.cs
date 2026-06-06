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
    /// Recomputes tenant-wide and all site/department/manager-scoped hotspot aggregates.
    /// </summary>
    public async Task RecalculateAllHotspotsAsync(string tenantId, CancellationToken ct = default)
    {
        await RecalculateTenantHotspotAsync(tenantId, ct);
        await RecalculateScopedHotspotsAsync(tenantId, ct);
    }

    /// <summary>
    /// Recomputes hotspots for every site, department, and manager scope that has
    /// at least one employee registered in <c>Intel_EmployeeScopes</c>.
    /// </summary>
    public async Task RecalculateScopedHotspotsAsync(string tenantId, CancellationToken ct = default)
    {
        var scopes = await _db.WorkforceEmployeeScopes
            .Where(s => s.TenantId == tenantId)
            .Select(s => new { s.EmployeeId, s.SiteCode, s.DepartmentCode, s.ManagerId })
            .ToListAsync(ct);

        if (scopes.Count == 0) return;

        var allPairs = await _db.WorkforceBurnoutScores
            .Where(b => b.TenantId == tenantId)
            .Join(
                _db.WorkforceAttritionScores.Where(a => a.TenantId == tenantId),
                b => b.EmployeeId,
                a => a.EmployeeId,
                (b, a) => new { b.EmployeeId, BurnoutScore = b.Score, AttritionScore = a.Score })
            .ToListAsync(ct);

        var pairByEmployee = allPairs.ToDictionary(p => p.EmployeeId);

        async Task UpsertScopeAsync(string scopeKey, IEnumerable<Guid> employeeIds)
        {
            var pairs = employeeIds
                .Where(id => pairByEmployee.ContainsKey(id))
                .Select(id => new EmployeeScorePair(pairByEmployee[id].BurnoutScore, pairByEmployee[id].AttritionScore))
                .ToList();

            if (pairs.Count == 0) return;

            var result = HotspotCalculator.Calculate(pairs);

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
        }

        var bySite = scopes.Where(s => s.SiteCode != null).GroupBy(s => s.SiteCode!).ToList();
        foreach (var group in bySite)
        {
            try { await UpsertScopeAsync($"site:{group.Key}", group.Select(s => s.EmployeeId)); }
            catch (Exception ex) { _logger.LogWarning(ex, "Hotspot failed for site:{SiteCode}", group.Key); }
        }

        var byDept = scopes.Where(s => s.DepartmentCode != null).GroupBy(s => s.DepartmentCode!).ToList();
        foreach (var group in byDept)
        {
            try { await UpsertScopeAsync($"dept:{group.Key}", group.Select(s => s.EmployeeId)); }
            catch (Exception ex) { _logger.LogWarning(ex, "Hotspot failed for dept:{DeptCode}", group.Key); }
        }

        var byManager = scopes.Where(s => s.ManagerId != null).GroupBy(s => s.ManagerId!).ToList();
        foreach (var group in byManager)
        {
            try { await UpsertScopeAsync($"manager:{group.Key}", group.Select(s => s.EmployeeId)); }
            catch (Exception ex) { _logger.LogWarning(ex, "Hotspot failed for manager:{ManagerId}", group.Key); }
        }

        _logger.LogInformation(
            "Scoped hotspots: {Sites} sites, {Depts} depts, {Managers} managers — tenant {TenantId}",
            bySite.Count, byDept.Count, byManager.Count, tenantId);
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
