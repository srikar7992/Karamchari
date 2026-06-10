// -----------------------------------------------------------------------
// <copyright file="AttritionScoreCalculator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Intelligence.Domain.Signals;
using Karamchari.Intelligence.Domain.Workforce;

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Deterministic attrition risk score calculator.
/// Pure static — no I/O, no dependencies.
///
/// Signal weights (must sum to 1.0):
///   AttendanceDegradation   25%
///   LeaveFrequencyChange    20%
///   SickLeaveSpike          20%
///   ShiftSwapPattern        10%
///   OvertimeRejection       10%
///   PeerComparison          10%
///   ManagerFriction          5%
/// </summary>
public static class AttritionScoreCalculator
{
    // ── Weights ───────────────────────────────────────────────────────────────
    private const decimal W_AttendanceDeg = 0.25m;
    private const decimal W_LeaveFreq = 0.20m;
    private const decimal W_SickLeave = 0.20m;
    private const decimal W_ShiftSwap = 0.10m;
    private const decimal W_OtRejection = 0.10m;
    private const decimal W_PeerComparison = 0.10m;
    private const decimal W_ManagerFriction = 0.05m;

    private const decimal MaxPoints = 35m;

    /// <summary>Calculates a deterministic attrition risk score in [0, 100].</summary>
    public static AttritionScoreResult Calculate(AttritionSignalInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var components = new Dictionary<string, decimal>();

        // 1. Attendance degradation (slope-based)
        components["AttendanceDegradation"] = NormalisePts(AttendanceSlopePoints(input.LateArrivalSlope)) * W_AttendanceDeg * 100m;

        // 2. Leave frequency change
        components["LeaveFrequencyChange"] = NormalisePts(LeaveFrequencyPoints(input.LeaveFrequencyRatio)) * W_LeaveFreq * 100m;

        // 3. Sick leave spike
        components["SickLeaveSpike"] = NormalisePts(SickLeavePoints(input.SickLeaveDays30d, input.HistoricalSickLeaveBaseline)) * W_SickLeave * 100m;

        // 4. Shift swap pattern
        components["ShiftSwapPattern"] = NormalisePts(ShiftSwapPoints(input.ShiftSwaps30d)) * W_ShiftSwap * 100m;

        // 5. Overtime rejection
        components["OvertimeRejection"] = NormalisePts(OvertimeRejectionPoints(input.OvertimeRejections30d)) * W_OtRejection * 100m;

        // 6. Peer comparison (attendance gap)
        components["PeerComparison"] = NormalisePts(PeerGapPoints(input.PeerAttendanceGap)) * W_PeerComparison * 100m;

        // 7. Manager friction
        components["ManagerFriction"] = NormalisePts(ManagerFrictionPoints(input.ManagerFrictionScore)) * W_ManagerFriction * 100m;

        var rawScore = components.Values.Sum();
        var score = Math.Clamp(Math.Round(rawScore, 1), 0m, 100m);
        var risk = ScoreToRiskLevel(score);
        var confidence = DataAgeToConfidence(input.DataAgeDays, input.IsNewJoiner);
        var explanation = BuildExplanation(input, components, score, risk);

        return new AttritionScoreResult(score, risk, confidence, explanation, components);
    }

    // ── Signal point functions ────────────────────────────────────────────────

    /// <summary>
    /// Late-arrival slope (arrivals per month increase):
    /// ≤0 = stable/improving (0 pts); 0–1 = 5; 1–3 = 15; 3+ = 35
    /// </summary>
    public static decimal AttendanceSlopePoints(decimal slope) => slope switch
    {
        <= 0m => 0m,
        <= 1m => 5m,
        <= 3m => 15m,
        _ => 35m
    };

    /// <summary>
    /// Leave frequency ratio (recent / historical):
    /// ≤1.0 = no change (0); 1.0–1.5 = 10; 1.5–2.5 = 20; &gt;2.5 = 35
    /// </summary>
    public static decimal LeaveFrequencyPoints(decimal ratio) => ratio switch
    {
        <= 1.0m => 0m,
        <= 1.5m => 10m,
        <= 2.5m => 20m,
        _ => 35m
    };

    /// <summary>
    /// Sick leave spike (recent vs baseline):
    /// &lt;1.5× baseline = 0; 1.5–2× = 10; 2–3× = 20; &gt;3× = 35
    /// </summary>
    public static decimal SickLeavePoints(decimal sickDays30d, decimal baseline)
    {
        if (baseline <= 0m) return sickDays30d > 0m ? 20m : 0m;
        var ratio = sickDays30d / baseline;
        return ratio switch
        {
            < 1.5m => 0m,
            < 2.0m => 10m,
            < 3.0m => 20m,
            _ => 35m
        };
    }

    /// <summary>Shift swaps per 30d attrition pattern: 0–2 = 0; 3–5 = 15; 6+ = 35</summary>
    public static decimal ShiftSwapPoints(int swaps30d) => swaps30d switch
    {
        <= 2 => 0m,
        <= 5 => 15m,
        _ => 35m
    };

    /// <summary>OT rejections in 30d: 0–1 = 0; 2–3 = 15; 4+ = 35</summary>
    public static decimal OvertimeRejectionPoints(int rejections30d) => rejections30d switch
    {
        <= 1 => 0m,
        <= 3 => 15m,
        _ => 35m
    };

    /// <summary>
    /// Peer attendance gap (employee % − team %):
    /// ≥0 = at or above team (0); -10%–0 = 10; -25%– -10% = 20; &lt;-25% = 35
    /// </summary>
    public static decimal PeerGapPoints(decimal gapPercent) => gapPercent switch
    {
        >= 0m => 0m,
        >= -10m => 10m,
        >= -25m => 20m,
        _ => 35m
    };

    /// <summary>Manager friction score [0–1]: &lt;0.2 = 0; 0.2–0.5 = 15; &gt;0.5 = 35</summary>
    public static decimal ManagerFrictionPoints(decimal score) => score switch
    {
        < 0.2m => 0m,
        < 0.5m => 15m,
        _ => 35m
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    public static decimal NormalisePts(decimal pts) => pts / MaxPoints;

    public static WorkforceRiskLevel ScoreToRiskLevel(decimal score) => score switch
    {
        < 30m => WorkforceRiskLevel.Low,
        < 60m => WorkforceRiskLevel.Moderate,
        < 80m => WorkforceRiskLevel.High,
        _ => WorkforceRiskLevel.Critical
    };

    public static WorkforceScoreConfidence DataAgeToConfidence(int ageDays, bool isNewJoiner)
    {
        if (isNewJoiner || ageDays < 60) return WorkforceScoreConfidence.Low;
        if (ageDays < 90) return WorkforceScoreConfidence.Medium;
        return WorkforceScoreConfidence.High;
    }

    private static ScoreExplanation BuildExplanation(
        AttritionSignalInput input,
        Dictionary<string, decimal> components,
        decimal score,
        WorkforceRiskLevel risk)
    {
        var contributors = components
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new ContributingFactor(kv.Key, kv.Value, EvidenceType.SystemCalculated,
                $"Contributed {kv.Value:F1} points to attrition score"))
            .ToList();

        var penalties = new List<PenalizingFactor>();
        if (input.IsNewJoiner)
            penalties.Add(new PenalizingFactor("NewJoiner", 0m, "Less than 60 days history — attrition score is indicative only"));

        var missing = new List<MissingEvidence>();
        if (input.DataAgeDays < 90)
            missing.Add(new MissingEvidence("FullHistory", "90+ days required for stable attrition trending"));

        var topDriver = components.OrderByDescending(x => x.Value).FirstOrDefault();
        var summary = $"Attrition risk {score:F0}/100 ({risk}). " +
                      $"Top driver: {topDriver.Key}. " +
                      $"Leave frequency ratio: {input.LeaveFrequencyRatio:F2}×, sick leave (30d): {input.SickLeaveDays30d}.";

        return ScoreExplanation.Compile(contributors, penalties, missing, summary);
    }
}
