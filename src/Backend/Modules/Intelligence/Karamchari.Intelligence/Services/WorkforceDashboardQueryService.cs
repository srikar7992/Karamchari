using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Intelligence.Services;

// ── DTOs ────────────────────────────────────────────────────────────────────

/// <summary>Hotspot summary for the dashboard.</summary>
public sealed record HotspotSummaryDto(
    string ScopeKey,
    int TotalEmployees,
    decimal MeanBurnoutScore,
    decimal MeanAttritionScore,
    decimal HighCriticalBurnoutPct,
    decimal HighCriticalAttritionPct,
    string BurnoutSeverity,
    string AttritionSeverity,
    DateTime ComputedAt);

/// <summary>Employee forecast flagged as at-risk.</summary>
public sealed record EmployeeAtRiskForecastDto(
    Guid EmployeeId,
    string ScoreType,
    decimal CurrentScore,
    decimal ProjectedScore,
    int? DaysUntilCritical,
    string Trend,
    string RiskLevel);

/// <summary>Employee dependency risk summary.</summary>
public sealed record DependencyRiskSummaryDto(
    Guid EmployeeId,
    decimal Score,
    string RiskLevel,
    decimal CriticalShiftComponent,
    decimal CrossTrainingComponent,
    decimal UniqueRoleComponent,
    decimal ApprovalComponent,
    DateTime ComputedAt);

/// <summary>Coverage fragility summary per scope.</summary>
public sealed record CoverageFragilitySummaryDto(
    string ScopeKey,
    decimal FragilityScore,
    string FragilityLevel,
    int? DaysUntilCoverageFailure,
    int ScopeEmployeeCount,
    DateTime ComputedAt);

/// <summary>Recommendation open/resolved counts with effectiveness.</summary>
public sealed record RecommendationSummaryDto(
    int OpenCount,
    int HighPriorityCount,
    int ResolvedLast30dCount,
    int EffectiveInterventions,
    int TotalEvaluatedInterventions,
    decimal OverallEffectivenessRate);

/// <summary>Employee causal chain summary for employees with Moderate or Severe chains.</summary>
public sealed record CausalChainAlertDto(
    Guid EmployeeId,
    int ActiveLinkCount,
    string ChainSeverity,
    IReadOnlyList<string> ActiveLinks,
    DateTime ComputedAt);

// ── Service ──────────────────────────────────────────────────────────────────

/// <summary>
/// Read-only query service for the workforce intelligence executive dashboard.
/// Returns pre-computed aggregates from existing Intelligence module tables.
/// Consumed by <c>WorkforceIntelligenceEndpoints</c> in the API host.
/// </summary>
public sealed class WorkforceDashboardQueryService
{
    private readonly IntelligenceDbContext _db;

    public WorkforceDashboardQueryService(IntelligenceDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns all hotspot aggregates for the tenant, optionally filtered by scope prefix.
    /// </summary>
    /// <param name="tenantId">Tenant to query.</param>
    /// <param name="scopeFilter">Optional prefix filter: "tenant", "site", "dept", "manager".</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<HotspotSummaryDto>> GetHotspotsAsync(
        string tenantId, string? scopeFilter, CancellationToken ct = default)
    {
        var query = _db.WorkforceHotspots
            .AsNoTracking()
            .Where(h => h.TenantId == tenantId);

        if (!string.IsNullOrEmpty(scopeFilter))
            query = query.Where(h => h.ScopeKey.StartsWith(scopeFilter + ":"));

        return await query
            .OrderByDescending(h => h.MeanBurnoutScore)
            .Select(h => new HotspotSummaryDto(
                h.ScopeKey,
                h.TotalEmployees,
                h.MeanBurnoutScore,
                h.MeanAttritionScore,
                h.HighCriticalBurnoutPct,
                h.HighCriticalAttritionPct,
                h.BurnoutSeverity.ToString(),
                h.AttritionSeverity.ToString(),
                h.ComputedAt))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns employees whose burnout or attrition forecast crosses Critical within 30 days.
    /// </summary>
    public async Task<List<EmployeeAtRiskForecastDto>> GetHighRiskForecastsAsync(
        string tenantId, CancellationToken ct = default)
    {
        return await _db.WorkforceForecasts
            .AsNoTracking()
            .Where(f => f.TenantId == tenantId
                && (f.DaysUntilCritical != null && f.DaysUntilCritical <= 30
                    || f.ProjectedScore >= 80m))
            .OrderBy(f => f.DaysUntilCritical)
            .Select(f => new EmployeeAtRiskForecastDto(
                f.EmployeeId,
                f.ScoreType,
                f.CurrentScore,
                f.ProjectedScore,
                f.DaysUntilCritical,
                f.Trend.ToString(),
                f.Confidence.ToString()))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns the top <paramref name="topN"/> employees by dependency risk score.
    /// </summary>
    public async Task<List<DependencyRiskSummaryDto>> GetTopDependencyRisksAsync(
        string tenantId, int topN = 20, CancellationToken ct = default)
    {
        return await _db.EmployeeDependencyScores
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.RiskLevel != WorkforceRiskLevel.Low)
            .OrderByDescending(d => d.Score)
            .Take(topN)
            .Select(d => new DependencyRiskSummaryDto(
                d.EmployeeId,
                d.Score,
                d.RiskLevel.ToString(),
                d.CriticalShiftComponent,
                d.CrossTrainingComponent,
                d.UniqueRoleComponent,
                d.ApprovalComponent,
                d.ComputedAt))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns all coverage fragility records for the tenant, optionally filtered by scope.
    /// </summary>
    public async Task<List<CoverageFragilitySummaryDto>> GetCoverageFragilityAsync(
        string tenantId, string? scopeFilter, CancellationToken ct = default)
    {
        var query = _db.SiteCoverageFragilities
            .AsNoTracking()
            .Where(f => f.TenantId == tenantId);

        if (!string.IsNullOrEmpty(scopeFilter))
            query = query.Where(f => f.ScopeKey.StartsWith(scopeFilter + ":"));

        return await query
            .OrderByDescending(f => f.FragilityScore)
            .Select(f => new CoverageFragilitySummaryDto(
                f.ScopeKey,
                f.FragilityScore,
                f.FragilityLevel.ToString(),
                f.DaysUntilCoverageFailure,
                f.ScopeEmployeeCount,
                f.ComputedAt))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns a summary of open recommendations, effectiveness, and recent resolution counts.
    /// </summary>
    public async Task<RecommendationSummaryDto> GetRecommendationSummaryAsync(
        string tenantId, CancellationToken ct = default)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var openCount = await _db.WorkforceRecommendations
            .CountAsync(r => r.TenantId == tenantId && r.ResolvedAt == null, ct);

        var highPriorityCount = await _db.WorkforceRecommendations
            .CountAsync(r => r.TenantId == tenantId && r.ResolvedAt == null
                && (r.Priority == RecommendationPriority.High || r.Priority == RecommendationPriority.Critical), ct);

        var resolvedLast30d = await _db.WorkforceRecommendations
            .CountAsync(r => r.TenantId == tenantId
                && r.ResolvedAt != null && r.ResolvedAt >= thirtyDaysAgo, ct);

        var outcomes = await _db.InterventionOutcomes
            .Where(o => o.TenantId == tenantId && o.Status != InterventionOutcomeStatus.Undetermined)
            .Select(o => o.Status)
            .ToListAsync(ct);

        var effective = outcomes.Count(s =>
            s == InterventionOutcomeStatus.Effective || s == InterventionOutcomeStatus.PartiallyEffective);

        var effectivenessRate = outcomes.Count > 0
            ? Math.Round(effective / (decimal)outcomes.Count, 3)
            : 0m;

        return new RecommendationSummaryDto(
            OpenCount: openCount,
            HighPriorityCount: highPriorityCount,
            ResolvedLast30dCount: resolvedLast30d,
            EffectiveInterventions: effective,
            TotalEvaluatedInterventions: outcomes.Count,
            OverallEffectivenessRate: effectivenessRate);
    }

    /// <summary>
    /// Returns employees with Moderate or Severe active causal chains.
    /// </summary>
    public async Task<List<CausalChainAlertDto>> GetCausalChainAlertsAsync(
        string tenantId, CancellationToken ct = default)
    {
        var chains = await _db.EmployeeCausalChains
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId
                && (c.ChainSeverity == CausalChainSeverity.Moderate
                    || c.ChainSeverity == CausalChainSeverity.Severe))
            .OrderByDescending(c => c.ActiveLinkCount)
            .ToListAsync(ct);

        return chains
            .Select(c => new CausalChainAlertDto(
                c.EmployeeId,
                c.ActiveLinkCount,
                c.ChainSeverity.ToString(),
                c.GetActiveLinks().Select(l => l.ToString()).ToList(),
                c.ComputedAt))
            .ToList();
    }
}
