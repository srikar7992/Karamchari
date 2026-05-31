using Karamchari.PSA.Services;
using Xunit;

namespace Karamchari.PSA.Tests;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public class PricingEngineTests
{
    [Theory]
    [InlineData(625, 1000, 0.30, 892.86, 37.5, 30.0)]
    [InlineData(500, 600, 0.20, 625.00, 16.67, 20.0)]
    [InlineData(0, 500, 0.20, 500.00, 100.0, 20.0)] // Zero cost
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Recommend_ShouldCalculateOptimalRate(
        decimal cost,
        decimal currentRate,
        decimal targetMargin,
        decimal expectedOptimal,
        decimal expectedCurrentMargin,
        decimal expectedTargetMargin)
    {
        // Act
        var result = PricingEngine.Recommend(cost, currentRate, targetMargin);

        // Assert
        Assert.Equal(expectedOptimal, result.RecommendedRate);
        Assert.Equal(expectedCurrentMargin, result.MarginAtCurrentRate);
        Assert.Equal(expectedTargetMargin, result.MarginAtRecommendedRate);
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Recommend_ShouldThrow_WhenMarginIsInvalid()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => PricingEngine.Recommend(500, 600, 1.0m));
    }
}
