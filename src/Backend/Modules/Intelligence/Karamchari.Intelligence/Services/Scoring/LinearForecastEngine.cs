using Karamchari.Intelligence.Domain.Workforce;

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Linear trend extrapolation for workforce risk scores.
/// Extends VelocityCalculator's slope to project scores N days ahead.
/// Pure static — no I/O. Not ML — deterministic rule-based projection.
/// </summary>
public static class LinearForecastEngine
{
    private const decimal CriticalThreshold = 80m;
    private const int MinPointsForForecast = 3;

    /// <summary>
    /// Projects a score N days into the future using least-squares slope.
    /// </summary>
    /// <param name="history">Chronological (timestamp, score) pairs.</param>
    /// <param name="forecastHorizonDays">How many days ahead to project.</param>
    /// <returns>Forecast details including projected score and days-to-critical estimate.</returns>
    public static ForecastResult Compute(
        IReadOnlyList<(DateTime At, decimal Score)> history,
        int forecastHorizonDays = 21)
    {
        ArgumentNullException.ThrowIfNull(history);

        if (history.Count < MinPointsForForecast)
        {
            var current = history.Count > 0 ? history[^1].Score : 0m;
            return new ForecastResult(
                CurrentScore: current,
                ProjectedScore: current,
                ForecastHorizonDays: forecastHorizonDays,
                PointsPerDay: 0m,
                DaysUntilCritical: null,
                Trend: ScoreTrend.Stable,
                DataPointsUsed: history.Count,
                Confidence: WorkforceScoreConfidence.Low);
        }

        var velocity = VelocityCalculator.Compute(history);
        var latestScore = history.OrderBy(h => h.At).Last().Score;

        var rawProjected = latestScore + velocity.PointsPerDay * forecastHorizonDays;
        var projected = Math.Clamp(Math.Round(rawProjected, 1), 0m, 100m);

        int? daysUntilCritical = null;
        if (velocity.PointsPerDay > 0m && latestScore < CriticalThreshold)
        {
            var days = (CriticalThreshold - latestScore) / velocity.PointsPerDay;
            daysUntilCritical = days > 0 ? (int)Math.Ceiling(days) : null;
        }

        var confidence = history.Count >= 14
            ? WorkforceScoreConfidence.High
            : history.Count >= 7
                ? WorkforceScoreConfidence.Medium
                : WorkforceScoreConfidence.Low;

        return new ForecastResult(
            CurrentScore: latestScore,
            ProjectedScore: projected,
            ForecastHorizonDays: forecastHorizonDays,
            PointsPerDay: velocity.PointsPerDay,
            DaysUntilCritical: daysUntilCritical,
            Trend: velocity.Trend,
            DataPointsUsed: history.Count,
            Confidence: confidence);
    }
}

/// <summary>Output of LinearForecastEngine.Compute.</summary>
public sealed record ForecastResult(
    decimal CurrentScore,
    decimal ProjectedScore,
    int ForecastHorizonDays,
    decimal PointsPerDay,
    int? DaysUntilCritical,
    ScoreTrend Trend,
    int DataPointsUsed,
    WorkforceScoreConfidence Confidence);
