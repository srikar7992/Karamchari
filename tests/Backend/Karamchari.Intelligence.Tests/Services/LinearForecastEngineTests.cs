using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class LinearForecastEngineTests
{
    private static DateTime D(int daysAgo) => DateTime.UtcNow.AddDays(-daysAgo);

    [Fact]
    public void Compute_EmptyHistory_ReturnsZeroScoreLowConfidence()
    {
        var result = LinearForecastEngine.Compute(Array.Empty<(DateTime, decimal)>());
        result.CurrentScore.Should().Be(0m);
        result.ProjectedScore.Should().Be(0m);
        result.Confidence.Should().Be(WorkforceScoreConfidence.Low);
        result.DaysUntilCritical.Should().BeNull();
    }

    [Fact]
    public void Compute_LessThanThreePoints_ReturnsLowConfidenceStatic()
    {
        var history = new[] { (D(5), 60m), (D(0), 65m) };
        var result = LinearForecastEngine.Compute(history);
        // Less than MinPointsForForecast (3) — returns current score unchanged
        result.CurrentScore.Should().Be(65m);
        result.ProjectedScore.Should().Be(65m);
        result.Confidence.Should().Be(WorkforceScoreConfidence.Low);
        result.Trend.Should().Be(ScoreTrend.Stable);
    }

    [Fact]
    public void Compute_SteadyRise_ProjectsHigherScore()
    {
        // 1 pt/day slope, starting at 50 — project 21 days → ~71
        var history = new[]
        {
            (D(20), 50m),
            (D(10), 60m),
            (D(0),  70m)
        };
        var result = LinearForecastEngine.Compute(history, forecastHorizonDays: 10);
        result.ProjectedScore.Should().BeApproximately(80m, 2m);
        result.Trend.Should().Be(ScoreTrend.Deteriorating);
        result.PointsPerDay.Should().BeApproximately(1.0m, 0.05m);
    }

    [Fact]
    public void Compute_ProjectedScore_ClampedAt100()
    {
        var history = new[]
        {
            (D(20), 70m),
            (D(10), 85m),
            (D(0),  95m)
        };
        var result = LinearForecastEngine.Compute(history, forecastHorizonDays: 30);
        result.ProjectedScore.Should().BeLessOrEqualTo(100m);
    }

    [Fact]
    public void Compute_ProjectedScore_ClampedAt0WhenImproving()
    {
        var history = new[]
        {
            (D(20), 30m),
            (D(10), 15m),
            (D(0),  5m)
        };
        var result = LinearForecastEngine.Compute(history, forecastHorizonDays: 30);
        result.ProjectedScore.Should().BeGreaterOrEqualTo(0m);
    }

    [Fact]
    public void Compute_DaysUntilCritical_ComputedForRisingScore()
    {
        // slope ~1 pt/day, current = 50 → critical at 80 → 30 days
        var history = new[]
        {
            (D(20), 30m),
            (D(10), 40m),
            (D(0),  50m)
        };
        var result = LinearForecastEngine.Compute(history);
        result.DaysUntilCritical.Should().NotBeNull();
        result.DaysUntilCritical.Should().BeInRange(27, 33);
    }

    [Fact]
    public void Compute_DaysUntilCritical_NullWhenAlreadyCritical()
    {
        var history = new[]
        {
            (D(20), 75m),
            (D(10), 80m),
            (D(0),  85m)
        };
        var result = LinearForecastEngine.Compute(history);
        // Already Critical (85 ≥ 80) — no meaningful days-until
        result.DaysUntilCritical.Should().BeNull();
    }

    [Fact]
    public void Compute_DaysUntilCritical_NullWhenImproving()
    {
        var history = new[]
        {
            (D(20), 70m),
            (D(10), 60m),
            (D(0),  50m)
        };
        var result = LinearForecastEngine.Compute(history);
        result.DaysUntilCritical.Should().BeNull();
    }

    [Fact]
    public void Compute_14OrMorePoints_ReturnsHighConfidence()
    {
        var history = Enumerable.Range(0, 14)
            .Select(i => (D(13 - i), 50m + i))
            .ToList();
        var result = LinearForecastEngine.Compute(history);
        result.Confidence.Should().Be(WorkforceScoreConfidence.High);
        result.DataPointsUsed.Should().Be(14);
    }

    [Fact]
    public void Compute_7To13Points_ReturnsMediumConfidence()
    {
        var history = Enumerable.Range(0, 9)
            .Select(i => (D(8 - i), 50m + i))
            .ToList();
        var result = LinearForecastEngine.Compute(history);
        result.Confidence.Should().Be(WorkforceScoreConfidence.Medium);
    }
}
