using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Derives per-recommendation-type effectiveness rates from historical
/// <see cref="InterventionOutcome"/> records for a tenant.
///
/// Called once per tenant nightly by <see cref="WorkforceIntelligenceRecomputeJob"/>
/// before the per-employee scoring pass, so the rates can be used to rank
/// recommendations that cycle generates.
///
/// Minimum sample: a type needs at least <see cref="MinimumSampleSize"/> evaluated
/// outcomes before its effectiveness rate is surfaced. Types below the threshold
/// receive a default rate of 0.5 (neutral; no preference applied).
/// </summary>
public sealed class RecommendationEffectivenessService
{
    /// <summary>Minimum evaluated outcomes before a type's effectiveness rate is trusted.</summary>
    public const int MinimumSampleSize = 3;

    private readonly IntelligenceDbContext _db;
    private readonly ILogger<RecommendationEffectivenessService> _logger;

    public RecommendationEffectivenessService(
        IntelligenceDbContext db,
        ILogger<RecommendationEffectivenessService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Returns effectiveness rates per recommendation type for the tenant.
    /// Effectiveness = count(Effective + PartiallyEffective) / count(all determined) per type.
    /// Types below <see cref="MinimumSampleSize"/> are excluded (caller falls back to 0.5).
    /// </summary>
    public async Task<IReadOnlyDictionary<WorkforceRecommendationType, decimal>>
        GetEffectivenessRatesAsync(string tenantId, CancellationToken ct = default)
    {
        // Join outcomes to recommendations to get the recommendation type
        var outcomes = await _db.InterventionOutcomes
            .Where(o => o.TenantId == tenantId
                     && o.Status != InterventionOutcomeStatus.Undetermined)
            .Join(
                _db.WorkforceRecommendations.Where(r => r.TenantId == tenantId),
                o => o.RecommendationId,
                r => r.Id,
                (o, r) => new { r.Type, o.Status })
            .ToListAsync(ct);

        if (outcomes.Count == 0)
            return new Dictionary<WorkforceRecommendationType, decimal>();

        var grouped = outcomes
            .GroupBy(x => x.Type)
            .Where(g => g.Count() >= MinimumSampleSize)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var total = g.Count();
                    var effective = g.Count(x =>
                        x.Status == InterventionOutcomeStatus.Effective
                        || x.Status == InterventionOutcomeStatus.PartiallyEffective);
                    return Math.Round((decimal)effective / total, 3);
                });

        _logger.LogDebug(
            "Effectiveness rates computed for {Count} recommendation types in tenant {TenantId}",
            grouped.Count, tenantId);

        return grouped;
    }
}
