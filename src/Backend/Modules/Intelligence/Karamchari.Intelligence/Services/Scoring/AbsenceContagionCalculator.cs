using Karamchari.Intelligence.Domain.Workforce;

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Detects absence cluster outbreaks by comparing the current team absence rate
/// against the 90-day historical baseline using a proportional z-test.
///
/// Z-score thresholds:
///   &lt; 1.5  → Low    (normal variation)
///   1.5–2.5 → Moderate (elevated, monitor)
///   2.5–4.0 → High   (probable cluster)
///   &gt; 4.0  → Critical (outbreak — immediate action)
/// </summary>
public static class AbsenceContagionCalculator
{
    /// <summary>Calculates absence contagion risk level and z-score for a team.</summary>
    public static AbsenceContagionResult Calculate(AbsenceContagionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        decimal zScore;
        if (input.TeamSize < 2 || input.BaselineAbsenceRate <= 0m)
        {
            // Not enough data for a meaningful test
            zScore = 0m;
        }
        else
        {
            // Proportional z-test: z = (p̂ − p₀) / sqrt(p₀(1−p₀)/n)
            var p0 = input.BaselineAbsenceRate;
            var stdError = (decimal)Math.Sqrt((double)(p0 * (1m - p0) / input.TeamSize));
            zScore = stdError > 0m
                ? (input.CurrentAbsenceRate - p0) / stdError
                : 0m;
        }

        var riskLevel = Math.Abs(zScore) switch
        {
            < 1.5m => WorkforceRiskLevel.Low,
            < 2.5m => WorkforceRiskLevel.Moderate,
            < 4.0m => WorkforceRiskLevel.High,
            _ => WorkforceRiskLevel.Critical
        };

        return new AbsenceContagionResult(
            CurrentAbsenceRate: input.CurrentAbsenceRate,
            BaselineAbsenceRate: input.BaselineAbsenceRate,
            ZScore: Math.Round(zScore, 2),
            RiskLevel: riskLevel,
            TeamSize: input.TeamSize);
    }
}

/// <summary>Input for AbsenceContagionCalculator.</summary>
public sealed record AbsenceContagionInput(
    decimal CurrentAbsenceRate,
    decimal BaselineAbsenceRate,
    int TeamSize);

/// <summary>Output of AbsenceContagionCalculator.Calculate.</summary>
public sealed record AbsenceContagionResult(
    decimal CurrentAbsenceRate,
    decimal BaselineAbsenceRate,
    decimal ZScore,
    WorkforceRiskLevel RiskLevel,
    int TeamSize);
