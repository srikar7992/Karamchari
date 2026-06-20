// -----------------------------------------------------------------------
// <copyright file="InterventionEffectivenessRefreshService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Intelligence.Domain.Interventions;
using Karamchari.Intelligence.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Refreshes acceptance-rate fields on <see cref="InterventionEffectiveness"/> records
/// by aggregating <see cref="RecommendationDisposition"/> data nightly.
///
/// Acceptance Rate = Accepted / (Accepted + Declined) per template.
/// Deferred and Expired dispositions are excluded from the denominator because they
/// do not represent a definitive accept/reject decision.
///
/// Acceptance rate is stored at template level (not template × signal) because
/// <see cref="Workforce.WorkforceRecommendation"/> does not carry a SignalType.
/// The same rate is written to all InterventionEffectiveness rows for a given template.
///
/// Called once per tenant by <see cref="WorkforceIntelligenceRecomputeJob"/>.
/// Idempotent — overwrites existing values on every run.
/// </summary>
public sealed class InterventionEffectivenessRefreshService
{
    private readonly IntelligenceDbContext _db;
    private readonly ILogger<InterventionEffectivenessRefreshService> _logger;

    public InterventionEffectivenessRefreshService(
        IntelligenceDbContext db,
        ILogger<InterventionEffectivenessRefreshService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Aggregates dispositions for all templates in a tenant and updates acceptance
    /// rate fields on existing <see cref="InterventionEffectiveness"/> rows.
    /// </summary>
    public async Task RefreshTenantAsync(string tenantId, CancellationToken ct = default)
    {
        // Aggregate Accepted + Declined dispositions per TemplateId.
        // Inner join to WorkforceRecommendations filters out recommendations with no TemplateId.
        var summaries = await _db.RecommendationDispositions
            .Where(d => d.TenantId == tenantId
                     && (d.Disposition == RecommendationDispositionType.Accepted
                      || d.Disposition == RecommendationDispositionType.Declined))
            .Join(
                _db.WorkforceRecommendations
                    .Where(r => r.TenantId == tenantId && r.TemplateId != null),
                d => d.RecommendationId,
                r => r.Id,
                (d, r) => new { r.TemplateId, d.Disposition })
            .GroupBy(x => x.TemplateId)
            .Select(g => new
            {
                TemplateId = g.Key!.Value,
                AcceptedCount = g.Count(x => x.Disposition == RecommendationDispositionType.Accepted),
                TotalCount = g.Count()
            })
            .ToListAsync(ct);

        if (summaries.Count == 0)
        {
            _logger.LogDebug(
                "No disposition data for tenant {TenantId} — acceptance rates unchanged",
                tenantId);
            return;
        }

        var templateIds = summaries.Select(s => s.TemplateId).ToList();

        var effectivenessRows = await _db.InterventionEffectiveness
            .Where(e => e.TenantId == tenantId && templateIds.Contains(e.TemplateId))
            .ToListAsync(ct);

        if (effectivenessRows.Count == 0) return;

        var summaryIndex = summaries.ToDictionary(s => s.TemplateId);

        foreach (var row in effectivenessRows)
        {
            if (!summaryIndex.TryGetValue(row.TemplateId, out var summary)) continue;
            row.UpdateAcceptance(summary.AcceptedCount, summary.TotalCount);
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogDebug(
            "Acceptance rates refreshed for {TemplateCount} templates ({RowCount} effectiveness rows) in tenant {TenantId}",
            summaries.Count, effectivenessRows.Count, tenantId);
    }
}
