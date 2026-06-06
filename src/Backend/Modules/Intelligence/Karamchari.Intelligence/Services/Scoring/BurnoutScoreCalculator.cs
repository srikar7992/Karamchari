using Karamchari.Intelligence.Domain.Signals;
using Karamchari.Intelligence.Domain.Workforce;

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Deterministic burnout score calculator.
/// Pure static — no I/O, no dependencies. Call it from tests directly.
///
/// Signal weights (must sum to 1.0):
///   ConsecutiveWorkDays     25%
///   ExcessiveOvertime       20%
///   LeaveAvoidance          15%
///   AttendanceDegradation   15%
///   ShiftVolatility         10%
///   HighIntensityExposure   10%
///   CoveragePressure         5%
///
/// Each signal produces a raw "points" value on a 0–35 scale,
/// which is normalised to 0–100 by dividing by the maximum possible
/// raw contribution for that signal.
/// </summary>
public static class BurnoutScoreCalculator
{
    // ── Weight constants ─────────────────────────────────────────────────────
    private const decimal W_ConsecutiveDays = 0.25m;
    private const decimal W_Overtime = 0.20m;
    private const decimal W_LeaveAvoidance = 0.15m;
    private const decimal W_AttendanceDegradation = 0.15m;
    private const decimal W_ShiftVolatility = 0.10m;
    private const decimal W_HighIntensity = 0.10m;
    private const decimal W_CoveragePressure = 0.05m;

    // ── Maximum raw points per signal (used to normalise to [0,100]) ─────────
    private const decimal MaxPoints = 35m; // signals top out at 35 pts

    // ── Thresholds ───────────────────────────────────────────────────────────
    private const int MinDirectReportDays = 60; // below = IsNewJoiner path

    /// <summary>
    /// Calculates a deterministic burnout score in [0, 100].
    /// </summary>
    public static BurnoutScoreResult Calculate(BurnoutSignalInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var components = new Dictionary<string, decimal>();

        // 1. Consecutive work days
        var consecutivePts = ConsecutiveDayPoints(input.ConsecutiveWorkDays);
        components["ConsecutiveWorkDays"] = NormalisePts(consecutivePts) * W_ConsecutiveDays * 100m;

        // 2. Excessive overtime (rolling 28d)
        var otPts = OvertimePoints(input.OvertimeHours28d);
        components["ExcessiveOvertime"] = NormalisePts(otPts) * W_Overtime * 100m;

        // 3. Leave avoidance
        var leavePts = LeaveAvoidancePoints(input.DaysWithoutLeave);
        components["LeaveAvoidance"] = NormalisePts(leavePts) * W_LeaveAvoidance * 100m;

        // 4. Attendance degradation (3-month late-arrival trend)
        var degradePts = AttendanceDegradationPoints(
            input.LateArrivalsMonthMinus2,
            input.LateArrivalsPreviousMonth,
            input.LateArrivalsCurrentMonth);
        components["AttendanceDegradation"] = NormalisePts(degradePts) * W_AttendanceDegradation * 100m;

        // 5. Shift volatility (swap frequency)
        var volatilityPts = ShiftVolatilityPoints(input.ShiftSwaps30d);
        components["ShiftVolatility"] = NormalisePts(volatilityPts) * W_ShiftVolatility * 100m;

        // 6. High intensity exposure
        var intensityPts = HighIntensityPoints(input.HighIntensityShiftRatio);
        components["HighIntensityExposure"] = NormalisePts(intensityPts) * W_HighIntensity * 100m;

        // 7. Coverage pressure
        var coveragePts = CoveragePressurePoints(input.EmergencyFillIns90d);
        components["CoveragePressure"] = NormalisePts(coveragePts) * W_CoveragePressure * 100m;

        var rawScore = components.Values.Sum();
        var score = Math.Clamp(Math.Round(rawScore, 1), 0m, 100m);
        var risk = ScoreToRiskLevel(score);
        var confidence = DataAgeToConfidence(input.DataAgeDays, input.IsNewJoiner);

        var explanation = BuildExplanation(input, components, score, risk);

        return new BurnoutScoreResult(score, risk, confidence, explanation, components);
    }

    // ── Signal point functions ────────────────────────────────────────────────

    /// <summary>0-5 days = 0 pts; 6 = 5; 7 = 10; 8 = 20; 9+ = 35</summary>
    public static decimal ConsecutiveDayPoints(int days) => days switch
    {
        <= 5 => 0m,
        6 => 5m,
        7 => 10m,
        8 => 20m,
        _ => 35m
    };

    /// <summary>&lt;10h = 0; 10–20h = 10; 20–40h = 20; &gt;40h = 35</summary>
    public static decimal OvertimePoints(decimal hours) => hours switch
    {
        < 10m => 0m,
        < 20m => 10m,
        < 40m => 20m,
        _ => 35m
    };

    /// <summary>&lt;90d = 0; 90–180d = 10; 180–270d = 20; &gt;270d = 35</summary>
    public static decimal LeaveAvoidancePoints(int daysWithoutLeave) => daysWithoutLeave switch
    {
        < 90 => 0m,
        < 180 => 10m,
        < 270 => 20m,
        _ => 35m
    };

    /// <summary>
    /// Scores attendance degradation from a 3-month late-arrival trend.
    /// Steady or improving = 0. Moderate increase = 10–20. Severe acceleration = 35.
    /// </summary>
    public static decimal AttendanceDegradationPoints(int monthMinus2, int monthMinus1, int current)
    {
        // Compute month-over-month deltas
        var delta1 = monthMinus1 - monthMinus2; // change from m-2 to m-1
        var delta2 = current - monthMinus1;     // change from m-1 to current

        // No data or stable
        if (delta1 <= 0 && delta2 <= 0) return 0m;

        var totalIncrease = Math.Max(0, current - monthMinus2);

        return totalIncrease switch
        {
            0 => 0m,
            <= 3 => 5m,
            <= 7 => 10m,
            <= 15 => 20m,
            _ => 35m
        };
    }

    /// <summary>Shift swaps per 30 days: 0–1 = 0; 2–3 = 10; 4–6 = 20; 7+ = 35</summary>
    public static decimal ShiftVolatilityPoints(int swaps30d) => swaps30d switch
    {
        <= 1 => 0m,
        <= 3 => 10m,
        <= 6 => 20m,
        _ => 35m
    };

    /// <summary>High-intensity shift ratio [0–1]: &lt;0.2 = 0; 0.2–0.4 = 10; 0.4–0.7 = 20; &gt;0.7 = 35</summary>
    public static decimal HighIntensityPoints(decimal ratio) => ratio switch
    {
        < 0.2m => 0m,
        < 0.4m => 10m,
        < 0.7m => 20m,
        _ => 35m
    };

    /// <summary>Emergency fill-ins in 90d: 0–2 = 0; 3–5 = 10; 6–9 = 20; 10+ = 35</summary>
    public static decimal CoveragePressurePoints(int fillIns90d) => fillIns90d switch
    {
        <= 2 => 0m,
        <= 5 => 10m,
        <= 9 => 20m,
        _ => 35m
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Normalises raw points [0, MaxPoints] to [0, 1].</summary>
    public static decimal NormalisePts(decimal pts)
        => pts / MaxPoints;

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
        BurnoutSignalInput input,
        Dictionary<string, decimal> components,
        decimal score,
        WorkforceRiskLevel risk)
    {
        var contributors = components
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new ContributingFactor(kv.Key, kv.Value, EvidenceType.SystemCalculated,
                $"Contributed {kv.Value:F1} points to burnout score"))
            .ToList();

        var penalties = new List<PenalizingFactor>();
        if (input.IsNewJoiner)
            penalties.Add(new PenalizingFactor("NewJoiner", 0m, "Less than 60 days of history — score confidence is Low"));

        var missing = new List<MissingEvidence>();
        if (input.DataAgeDays < 90)
            missing.Add(new MissingEvidence("FullHistory", "Score would be more reliable with 90+ days of data"));

        var summary = $"Burnout score {score:F0}/100 ({risk}). " +
                      $"Top driver: {components.OrderByDescending(x => x.Value).First().Key}. " +
                      $"Consecutive days: {input.ConsecutiveWorkDays}, OT hours (28d): {input.OvertimeHours28d:F1}.";

        return ScoreExplanation.Compile(contributors, penalties, missing, summary);
    }
}
