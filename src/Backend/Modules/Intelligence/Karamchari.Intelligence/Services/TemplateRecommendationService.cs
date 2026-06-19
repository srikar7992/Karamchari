// -----------------------------------------------------------------------
// <copyright file="TemplateRecommendationService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Intelligence.Domain.Interventions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Structured explanation of why a recommendation was generated.
/// Machine-readable companion to <see cref="RecommendationCandidate.Rationale"/> — each factor
/// is a named, typed value so the frontend can render "Contributing Factors" without string parsing.
///
/// <see cref="SignalCode"/> is the canonical enum name kept stable for programmatic use.
/// <see cref="SignalDescription"/> is the human-readable translation for display.
/// <see cref="IsDefaultRate"/> is true when no effectiveness history exists; the caller can
/// surface this as "Insufficient data — using neutral default".
/// </summary>
public sealed record ExplanationFactors(
    string SignalCode,
    string SignalDescription,
    decimal SignalIntensity,
    decimal HistoricalSuccessRate,
    int ObservedApplications,
    int MedianOutcomeDays,
    decimal Confidence,
    bool IsDefaultRate);

/// <summary>
/// Ephemeral projection of ranked intervention recommendations for an employee.
/// Not persisted — generated on demand from the current signal state and historical effectiveness.
/// </summary>
public sealed record RecommendationCandidate(
    Guid TemplateId,
    string TemplateName,
    InterventionCategory Category,
    decimal SuccessRate,
    decimal SignalIntensity,
    decimal CohortMatchMultiplier,
    decimal Score,
    string Rationale,
    ExplanationFactors Explanation);

/// <summary>
/// Generates ranked intervention recommendations from the template catalog using the V1 scoring formula:
///
///   Score = SuccessRate × SignalIntensity × CohortMatchMultiplier
///
/// SuccessRate       — from InterventionEffectiveness per (template × signal type).
///                     Defaults to 0.5 (neutral) when sample is insufficient.
/// SignalIntensity   — normalized risk score for the signal type (0..1).
/// CohortMatchMultiplier — similarity multiplier; always 1.0 in V1 until cohort tracking lands.
///
/// Returns an ephemeral <see cref="RecommendationCandidate"/> list sorted by Score desc.
/// The caller decides whether to persist recommendations as WorkforceRecommendation records.
/// </summary>
public sealed class TemplateRecommendationService
{
    private readonly IntelligenceDbContext _db;
    private readonly ILogger<TemplateRecommendationService> _logger;

    public TemplateRecommendationService(
        IntelligenceDbContext db,
        ILogger<TemplateRecommendationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Generates ranked candidates for the given employee and signal context.
    /// </summary>
    /// <param name="tenantId">Tenant scope.</param>
    /// <param name="employeeId">Employee at risk.</param>
    /// <param name="signalType">The primary signal driving the recommendation pass.</param>
    /// <param name="signalIntensity">
    /// Normalised signal strength (0..1).
    /// Callers should pass a normalized value, e.g. burnoutScore / 100.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IReadOnlyList<RecommendationCandidate>> GenerateAsync(
        string tenantId,
        Guid employeeId,
        WorkforceSignalType signalType,
        decimal signalIntensity,
        CancellationToken ct = default)
    {
        signalIntensity = Math.Clamp(signalIntensity, 0m, 1m);

        var templates = await _db.InterventionTemplates
            .Where(t => t.TenantId == tenantId && t.IsActive)
            .ToListAsync(ct);

        if (templates.Count == 0)
        {
            _logger.LogDebug(
                "No active templates for tenant {TenantId} — no template-driven candidates generated",
                tenantId);
            return [];
        }

        var effectivenessIndex = await _db.InterventionEffectiveness
            .Where(e => e.TenantId == tenantId && e.SignalType == signalType)
            .ToDictionaryAsync(e => e.TemplateId, ct);

        var candidates = new List<RecommendationCandidate>(templates.Count);

        foreach (var template in templates)
        {
            effectivenessIndex.TryGetValue(template.Id, out var eff);

            var successRate = eff?.SuccessRate ?? 0.5m;
            const decimal cohortMatch = 1.0m; // V1: neutral until cohort tracking is introduced
            var score = successRate * signalIntensity * cohortMatch;

            var explanation = new ExplanationFactors(
                SignalCode: signalType.ToString(),
                SignalDescription: DescribeSignal(signalType),
                SignalIntensity: signalIntensity,
                HistoricalSuccessRate: successRate,
                ObservedApplications: eff?.Applications ?? 0,
                MedianOutcomeDays: eff?.MedianOutcomeDays ?? 30,
                Confidence: eff?.Confidence ?? 0m,
                IsDefaultRate: eff is null);

            candidates.Add(new RecommendationCandidate(
                TemplateId: template.Id,
                TemplateName: template.Name,
                Category: template.Category,
                SuccessRate: successRate,
                SignalIntensity: signalIntensity,
                CohortMatchMultiplier: cohortMatch,
                Score: score,
                Rationale: BuildRationale(template.Name, successRate, signalIntensity, eff),
                Explanation: explanation));
        }

        var ranked = candidates.OrderByDescending(c => c.Score).ToList();

        _logger.LogDebug(
            "Generated {Count} candidates for employee {EmployeeId} signal {SignalType} (top score: {Top:F3})",
            ranked.Count, employeeId, signalType, ranked.FirstOrDefault()?.Score ?? 0m);

        return ranked;
    }

    private static string BuildRationale(
        string templateName,
        decimal successRate,
        decimal signalIntensity,
        InterventionEffectiveness? eff)
    {
        var confidence = eff != null
            ? $"{eff.Applications} prior applications, {eff.Confidence:P0} confidence"
            : "insufficient history — using neutral default";

        return $"Recommended because: '{templateName}' has {successRate:P0} historical effectiveness " +
               $"for this signal type ({confidence}). " +
               $"Signal intensity {signalIntensity:P0}.";
    }

    private static string DescribeSignal(WorkforceSignalType signalType) => signalType switch
    {
        // Burnout signals
        WorkforceSignalType.ConsecutiveWorkDays        => "Sustained multi-day run without a rest day",
        WorkforceSignalType.OvertimeHours28d           => "Elevated overtime over the last 28 days",
        WorkforceSignalType.DaysWithoutLeave           => "Extended period without approved leave",
        WorkforceSignalType.LateArrivalsMonthly        => "Increased late arrivals this month",
        WorkforceSignalType.ShiftSwaps30d              => "Frequent shift changes in the last 30 days",
        WorkforceSignalType.HighIntensityShiftRatio    => "High proportion of night, weekend, or holiday shifts",
        WorkforceSignalType.EmergencyFillIns90d        => "Repeated use as emergency coverage in the last 90 days",

        // Attrition signals
        WorkforceSignalType.LateArrivalSlope           => "Worsening late-arrival trend over 3 months",
        WorkforceSignalType.LeaveFrequencyRatio        => "Leave request frequency above personal baseline",
        WorkforceSignalType.SickLeaveDays30d           => "Above-average sick leave in the last 30 days",
        WorkforceSignalType.ShiftSwapAttritionScore    => "Shift-swap pattern suggests schedule dissatisfaction",
        WorkforceSignalType.OvertimeRejections30d      => "Increased overtime offer declines in the last 30 days",
        WorkforceSignalType.PeerAttendanceGap          => "Attendance rate below team average",
        WorkforceSignalType.ManagerFrictionScore       => "Elevated transfer requests, shift appeals, or escalations",

        // Phase 6.1
        WorkforceSignalType.CriticalShiftCoverageRatio => "High single-point dependency for critical shift coverage",

        // Phase 6.3 — Dependency Risk
        WorkforceSignalType.CrossTrainingDepth         => "Limited cross-role coverage capability",
        WorkforceSignalType.UniqueRoleFlag             => "Sole practitioner in this role with no trained backup",
        WorkforceSignalType.ApprovalAuthorityIndex     => "Holds significant approval authority with no backup",

        // Phase 6.3 — Schedule Quality
        WorkforceSignalType.NightToMorningTransitions7d => "Night-to-morning shift transitions without adequate rest",
        WorkforceSignalType.ShortRestViolations7d       => "Shifts preceded by fewer than 11 hours of rest",
        WorkforceSignalType.WeekendShiftClustering      => "Recurring concentration of weekend shifts",
        WorkforceSignalType.SplitShiftCount30d          => "Frequent split shifts in the last 30 days",

        _ => signalType.ToString()
    };
}
