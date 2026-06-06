using Karamchari.Intelligence.Domain.Workforce;

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Calculates schedule ergonomic quality — the upstream signal that predicts burnout
/// from scheduling patterns before burnout scores themselves spike.
/// Pure static — no I/O, directly unit-testable.
///
/// Outputs a QualityScore [0, 100] (higher = better ergonomics) and an inverted
/// PoorScheduleRiskLevel for comparison with burnout/attrition risk tiers.
///
/// Stress component weights:
///   RotationStress (night→morning)   35%
///   RestViolations                   35%
///   WeekendClustering                15%
///   SplitShifts                      15%
/// </summary>
public static class ScheduleQualityCalculator
{
    private const decimal MaxStress = 100m;

    // Night→morning rotation stress points
    private const decimal MaxRotationStress = 35m;

    // Short rest violation stress points
    private const decimal MaxRestStress = 35m;

    // Weekend clustering stress points
    private const decimal MaxWeekendStress = 15m;

    // Split shift stress points
    private const decimal MaxSplitStress = 15m;

    /// <summary>
    /// Calculates schedule quality from ergonomic signals.
    /// Null input throws <see cref="ArgumentNullException"/>.
    /// </summary>
    public static ScheduleQualityResult Calculate(ScheduleQualityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var rotationPts = NightToMorningStress(input.NightToMorningTransitions7d);
        var restPts = ShortRestStress(input.ShortRestViolations7d);
        var weekendPts = Math.Clamp(input.WeekendShiftClustering, 0m, 1m) * MaxWeekendStress;
        var splitPts = SplitShiftStress(input.SplitShiftCount30d);

        var totalStress = rotationPts + restPts + weekendPts + splitPts;
        var clampedStress = Math.Clamp(totalStress, 0m, MaxStress);

        var qualityScore = Math.Round(100m - clampedStress, 1);
        var riskLevel = QualityToRiskLevel(qualityScore);

        return new ScheduleQualityResult(
            QualityScore: qualityScore,
            PoorScheduleRiskLevel: riskLevel,
            RotationStressComponent: Math.Round(rotationPts, 1),
            RestViolationComponent: Math.Round(restPts, 1),
            WeekendClusterComponent: Math.Round(weekendPts, 1),
            SplitShiftComponent: Math.Round(splitPts, 1));
    }

    // ── Signal stress functions ───────────────────────────────────────────────

    /// <summary>0=0; 1=12; 2=24; 3+=35</summary>
    public static decimal NightToMorningStress(int transitions) => transitions switch
    {
        0 => 0m,
        1 => 12m,
        2 => 24m,
        _ => MaxRotationStress
    };

    /// <summary>0=0; 1=10; 2=22; 3+=35</summary>
    public static decimal ShortRestStress(int violations) => violations switch
    {
        0 => 0m,
        1 => 10m,
        2 => 22m,
        _ => MaxRestStress
    };

    /// <summary>0–2=0; 3–5=6; 6–9=10; 10+=15</summary>
    public static decimal SplitShiftStress(int splitShifts) => splitShifts switch
    {
        <= 2 => 0m,
        <= 5 => 6m,
        <= 9 => 10m,
        _ => MaxSplitStress
    };

    /// <summary>
    /// Maps quality score to risk level (inverted).
    /// Low quality = High/Critical schedule risk.
    /// </summary>
    public static WorkforceRiskLevel QualityToRiskLevel(decimal qualityScore) => qualityScore switch
    {
        >= 80m => WorkforceRiskLevel.Low,
        >= 60m => WorkforceRiskLevel.Moderate,
        >= 40m => WorkforceRiskLevel.High,
        _ => WorkforceRiskLevel.Critical
    };
}

/// <summary>Output of <see cref="ScheduleQualityCalculator.Calculate"/>.</summary>
public sealed record ScheduleQualityResult(
    decimal QualityScore,
    WorkforceRiskLevel PoorScheduleRiskLevel,
    decimal RotationStressComponent,
    decimal RestViolationComponent,
    decimal WeekendClusterComponent,
    decimal SplitShiftComponent);
