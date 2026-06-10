// -----------------------------------------------------------------------
// <copyright file="RecommendationEngine.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services.Scoring;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Generates workforce recommendations from calculated scores.
/// Pure static — takes scores, returns recommendation templates.
/// The orchestrator (WorkforceSignalService) persists them.
/// </summary>
public static class RecommendationEngine
{
    /// <summary>
    /// Derives recommendations from a burnout score.
    /// Returns an empty list if score is Low risk.
    /// If <paramref name="effectivenessRates"/> is provided, recommendations are sorted
    /// by historical effectiveness (highest first) so managers see the most impactful action first.
    /// </summary>
    public static IReadOnlyList<(WorkforceRecommendationType Type, RecommendationPriority Priority, string Rationale)>
        ForBurnout(
            BurnoutScoreResult result,
            BurnoutSignalInput input,
            IReadOnlyDictionary<WorkforceRecommendationType, decimal>? effectivenessRates = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(input);

        if (result.RiskLevel == WorkforceRiskLevel.Low)
            return [];

        var recs = new List<(WorkforceRecommendationType, RecommendationPriority, string)>();
        var priority = RiskToPriority(result.RiskLevel);

        if (input.OvertimeHours28d >= 20m)
            recs.Add((WorkforceRecommendationType.ReduceOvertime,
                priority,
                $"Employee logged {input.OvertimeHours28d:F1}h overtime in 28 days (burnout score {result.Score:F0}). Block OT for 14 days."));

        if (input.DaysWithoutLeave >= 90)
            recs.Add((WorkforceRecommendationType.ScheduleLeave,
                priority,
                $"No leave taken in {input.DaysWithoutLeave} days. Schedule leave to prevent burnout escalation."));

        if (input.ConsecutiveWorkDays >= 6)
            recs.Add((WorkforceRecommendationType.ReduceConsecutiveShifts,
                priority,
                $"{input.ConsecutiveWorkDays} consecutive shifts. Introduce a mandatory rest day within 2 days."));

        // Always add a monitor recommendation for Moderate cases with no other triggers
        if (recs.Count == 0 && result.RiskLevel == WorkforceRiskLevel.Moderate)
            recs.Add((WorkforceRecommendationType.MonitorAttendance,
                RecommendationPriority.Low,
                $"Burnout score {result.Score:F0} (Moderate). Continue monitoring — no immediate action required."));

        return RankByEffectiveness(recs, effectivenessRates);
    }

    /// <summary>
    /// Derives recommendations from an attrition score.
    /// Returns an empty list if score is Low risk.
    /// If <paramref name="effectivenessRates"/> is provided, recommendations are sorted
    /// by historical effectiveness (highest first).
    /// </summary>
    public static IReadOnlyList<(WorkforceRecommendationType Type, RecommendationPriority Priority, string Rationale)>
        ForAttrition(
            AttritionScoreResult result,
            AttritionSignalInput input,
            IReadOnlyDictionary<WorkforceRecommendationType, decimal>? effectivenessRates = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(input);

        if (result.RiskLevel == WorkforceRiskLevel.Low)
            return [];

        var recs = new List<(WorkforceRecommendationType, RecommendationPriority, string)>();
        var priority = RiskToPriority(result.RiskLevel);

        recs.Add((WorkforceRecommendationType.ManagerCheckIn,
            priority,
            $"Attrition risk {result.Score:F0}/100 ({result.RiskLevel}). Immediate manager 1-on-1 recommended."));

        if (input.ManagerFrictionScore >= 0.2m)
            recs.Add((WorkforceRecommendationType.TeamReassignmentReview,
                priority,
                $"Manager friction score {input.ManagerFrictionScore:F2}. Review team assignment or manager relationship."));

        recs.Add((WorkforceRecommendationType.WorkloadAnalysis,
            RecommendationPriority.Medium,
            $"Attrition risk elevated. Conduct workload analysis and compare against team peers."));

        return RankByEffectiveness(recs, effectivenessRates);
    }

    /// <summary>
    /// Derives recommendations for a manager from their effectiveness score.
    /// Returns an empty list for scores above 70.
    /// </summary>
    public static IReadOnlyList<(WorkforceRecommendationType Type, RecommendationPriority Priority, string Rationale)>
        ForManager(ManagerEffectivenessResult result, ManagerEffectivenessInput input)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(input);

        if (result.Score >= 70m)
            return [];

        var recs = new List<(WorkforceRecommendationType, RecommendationPriority, string)>();
        var priority = result.Score < 40m ? RecommendationPriority.High : RecommendationPriority.Medium;

        if (result.OvertimeDimension < 50m)
            recs.Add((WorkforceRecommendationType.ReduceOtReliance,
                priority,
                $"Team OT dependency is {input.AvgOvertimeHoursPerMember:F1}h/member vs org avg {input.OrgAvgOvertimeHoursPerEmployee:F1}h. Reduce reliance on overtime."));

        if (result.LeaveHealthDimension < 50m)
            recs.Add((WorkforceRecommendationType.IncreaseLeaveApproval,
                priority,
                $"Leave denial ratio {input.LeaveDenialRatio:P0} is above healthy threshold. Review leave approval practices."));

        if (result.BurnoutDimension < 50m || result.AttritionDimension < 50m)
            recs.Add((WorkforceRecommendationType.RebalanceStaffing,
                priority,
                $"High burnout/attrition in team. Review shift patterns and distribution of work."));

        return recs;
    }

    private static RecommendationPriority RiskToPriority(WorkforceRiskLevel risk) => risk switch
    {
        WorkforceRiskLevel.Moderate => RecommendationPriority.Medium,
        WorkforceRiskLevel.High => RecommendationPriority.High,
        WorkforceRiskLevel.Critical => RecommendationPriority.Critical,
        _ => RecommendationPriority.Low
    };

    /// <summary>
    /// Sorts a recommendation list by descending effectiveness rate.
    /// Types without a rate entry default to 0.5 (neutral) so they interleave
    /// without being penalised relative to unscored types.
    /// </summary>
    private static List<(WorkforceRecommendationType Type, RecommendationPriority Priority, string Rationale)>
        RankByEffectiveness(
            List<(WorkforceRecommendationType Type, RecommendationPriority Priority, string Rationale)> recs,
            IReadOnlyDictionary<WorkforceRecommendationType, decimal>? effectivenessRates)
    {
        if (effectivenessRates == null || effectivenessRates.Count == 0 || recs.Count <= 1)
            return recs;

        return recs
            .OrderByDescending(r => effectivenessRates.TryGetValue(r.Type, out var rate) ? rate : 0.5m)
            .ToList();
    }
}
