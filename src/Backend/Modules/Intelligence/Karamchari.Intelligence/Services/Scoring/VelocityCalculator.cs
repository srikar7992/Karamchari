using Karamchari.Intelligence.Domain.Workforce;

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Computes score velocity (rate of change) from a chronological series of score observations.
/// Pure static — no I/O.
/// </summary>
public static class VelocityCalculator
{
    /// <summary>Slope magnitude below which a score is considered Stable (points/day).</summary>
    private const decimal StableThreshold = 0.3m;

    /// <summary>
    /// Computes velocity and trend from an ordered list of (timestamp, score) pairs.
    /// Requires at least 2 data points; returns zero-velocity Stable if fewer.
    /// </summary>
    public static ScoreVelocity Compute(IReadOnlyList<(DateTime At, decimal Score)> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        if (history.Count < 2)
            return new ScoreVelocity(0m, 0, ScoreTrend.Stable, 0m);

        var ordered = history.OrderBy(h => h.At).ToList();
        var origin = ordered[0].At;

        // Least-squares linear regression over x = days since first observation
        int n = ordered.Count;
        decimal sumX = 0m, sumY = 0m, sumXY = 0m, sumXX = 0m;

        foreach (var (at, score) in ordered)
        {
            var x = (decimal)(at - origin).TotalDays;
            sumX += x;
            sumY += score;
            sumXY += x * score;
            sumXX += x * x;
        }

        var denominator = n * sumXX - sumX * sumX;
        var slope = denominator == 0m ? 0m : (n * sumXY - sumX * sumY) / denominator;

        var windowDays = (int)(ordered[^1].At - origin).TotalDays;
        if (windowDays == 0) windowDays = 1;

        var totalChange = ordered[^1].Score - ordered[0].Score;

        var trend = Math.Abs(slope) < StableThreshold
            ? ScoreTrend.Stable
            : slope > 0 ? ScoreTrend.Deteriorating : ScoreTrend.Improving;

        return new ScoreVelocity(
            PointsPerDay: Math.Round(slope, 3),
            WindowDays: windowDays,
            Trend: trend,
            TotalChangeInWindow: Math.Round(totalChange, 1));
    }

    /// <summary>
    /// Convenience overload for (CalculatedAt, Score) snapshots.
    /// Filters to the most recent <paramref name="maxDays"/> days.
    /// </summary>
    public static ScoreVelocity Compute(
        IReadOnlyList<(DateTime At, decimal Score)> history,
        int maxDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-maxDays);
        var window = history.Where(h => h.At >= cutoff).ToList();
        return Compute(window);
    }
}
