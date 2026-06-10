// -----------------------------------------------------------------------
// <copyright file="VelocityCalculatorTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class VelocityCalculatorTests
{
    private static DateTime D(int daysAgo) => DateTime.UtcNow.AddDays(-daysAgo);

    [Fact]
    public void Compute_EmptyHistory_ReturnsStableZero()
    {
        var result = VelocityCalculator.Compute(Array.Empty<(DateTime, decimal)>());
        result.PointsPerDay.Should().Be(0m);
        result.Trend.Should().Be(ScoreTrend.Stable);
        result.WindowDays.Should().Be(0);
    }

    [Fact]
    public void Compute_SinglePoint_ReturnsStableZero()
    {
        var result = VelocityCalculator.Compute(new[] { (D(10), 50m) });
        result.PointsPerDay.Should().Be(0m);
        result.Trend.Should().Be(ScoreTrend.Stable);
    }

    [Fact]
    public void Compute_RisingScores_ReturnsDeterioratingPositiveSlope()
    {
        // 0 → 10 → 20 → 30 over 30 days = slope ~1 pt/day
        var history = new[]
        {
            (D(30), 0m),
            (D(20), 10m),
            (D(10), 20m),
            (D(0),  30m)
        };
        var result = VelocityCalculator.Compute(history);
        result.Trend.Should().Be(ScoreTrend.Deteriorating);
        result.PointsPerDay.Should().BeApproximately(1.0m, 0.05m);
        result.TotalChangeInWindow.Should().Be(30m);
    }

    [Fact]
    public void Compute_FallingScores_ReturnsImproving()
    {
        var history = new[]
        {
            (D(20), 80m),
            (D(10), 60m),
            (D(0),  40m)
        };
        var result = VelocityCalculator.Compute(history);
        result.Trend.Should().Be(ScoreTrend.Improving);
        result.PointsPerDay.Should().BeLessThan(0m);
        result.TotalChangeInWindow.Should().Be(-40m);
    }

    [Fact]
    public void Compute_FlatScores_ReturnsStable()
    {
        var history = new[]
        {
            (D(20), 50m),
            (D(10), 50m),
            (D(0),  50m)
        };
        var result = VelocityCalculator.Compute(history);
        result.Trend.Should().Be(ScoreTrend.Stable);
        result.PointsPerDay.Should().Be(0m);
    }

    [Fact]
    public void Compute_WithMaxDaysFilter_OnlyUsesWindowSubset()
    {
        // First data point is old — should be excluded with maxDays = 15
        var history = new[]
        {
            (D(60), 10m),  // outside 15-day window
            (D(10), 50m),
            (D(0),  60m)
        };
        var withFilter = VelocityCalculator.Compute(history, maxDays: 15);
        var withoutFilter = VelocityCalculator.Compute(history);
        // With filter: only last 2 points (slope = 1 pt/day)
        withFilter.WindowDays.Should().BeLessThanOrEqualTo(15);
        withFilter.PointsPerDay.Should().BeApproximately(1.0m, 0.1m);
        // Without filter: all 3 points — different slope
        withoutFilter.WindowDays.Should().BeGreaterThan(15);
    }

    [Fact]
    public void Compute_WindowDays_ReflectsActualSpan()
    {
        var history = new[]
        {
            (D(14), 30m),
            (D(7),  40m),
            (D(0),  50m)
        };
        var result = VelocityCalculator.Compute(history);
        result.WindowDays.Should().Be(14);
    }
}
