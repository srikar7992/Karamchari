// -----------------------------------------------------------------------
// <copyright file="InterventionTrackerService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Evaluates resolved recommendations 30 days after resolution and persists
/// <see cref="InterventionOutcome"/> records.
///
/// Called by <see cref="WorkforceIntelligenceRecomputeJob"/> once nightly.
/// Idempotent — skips recommendations that already have an outcome record.
/// </summary>
public sealed class InterventionTrackerService
{
    /// <summary>Days after resolution before evaluating effectiveness.</summary>
    public const int EvaluationWindowDays = 30;

    private readonly IntelligenceDbContext _db;
    private readonly ILogger<InterventionTrackerService> _logger;

    public InterventionTrackerService(IntelligenceDbContext db, ILogger<InterventionTrackerService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates all mature resolved recommendations for a tenant.
    /// </summary>
    public async Task EvaluateTenantInterventionsAsync(string tenantId, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-EvaluationWindowDays);

        // Resolved recommendations old enough to evaluate, without existing outcome
        var candidates = await _db.WorkforceRecommendations
            .Where(r => r.TenantId == tenantId
                     && r.ResolvedAt != null
                     && r.ResolvedAt <= cutoff)
            .ToListAsync(ct);

        if (candidates.Count == 0) return;

        var existingOutcomeRecIds = await _db.InterventionOutcomes
            .Where(o => o.TenantId == tenantId)
            .Select(o => o.RecommendationId)
            .ToHashSetAsync(ct);

        var pending = candidates.Where(r => !existingOutcomeRecIds.Contains(r.Id)).ToList();

        if (pending.Count == 0) return;

        _logger.LogInformation("Evaluating {Count} mature interventions for tenant {TenantId}", pending.Count, tenantId);

        foreach (var rec in pending)
        {
            try
            {
                await EvaluateOneAsync(tenantId, rec, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Intervention evaluation failed for recommendation {RecId}", rec.Id);
            }
        }
    }

    private async Task EvaluateOneAsync(
        string tenantId,
        WorkforceRecommendation rec,
        CancellationToken ct)
    {
        // Determine which score type the recommendation was about.
        // Burnout recommendations have types prefixed or use burnout score; default heuristic:
        // if trigger score exists we need to compare current score of the right type.
        var scoreType = IsBurnoutRecommendationType(rec.Type) ? "Burnout" : "Attrition";

        decimal currentScore;
        if (scoreType == "Burnout")
        {
            currentScore = await _db.WorkforceBurnoutScores
                .Where(s => s.TenantId == tenantId && s.EmployeeId == rec.EmployeeId)
                .Select(s => s.Score)
                .FirstOrDefaultAsync(ct);
        }
        else
        {
            currentScore = await _db.WorkforceAttritionScores
                .Where(s => s.TenantId == tenantId && s.EmployeeId == rec.EmployeeId)
                .Select(s => s.Score)
                .FirstOrDefaultAsync(ct);
        }

        var days = (int)(DateTime.UtcNow - rec.CreatedAt).TotalDays;

        var outcome = InterventionOutcome.Evaluate(
            tenantId,
            rec.Id,
            rec.EmployeeId,
            scoreType,
            scoreAtRecommendation: rec.TriggerScore,
            scoreAtEvaluation: currentScore,
            daysAfterRecommendation: days);

        _db.InterventionOutcomes.Add(outcome);
        await _db.SaveChangesAsync(ct);
    }

    private static bool IsBurnoutRecommendationType(WorkforceRecommendationType type) => type switch
    {
        WorkforceRecommendationType.ScheduleLeave => true,
        WorkforceRecommendationType.ReduceOvertime => true,
        WorkforceRecommendationType.ReduceConsecutiveShifts => true,
        _ => false
    };
}
