using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class CoverageFragilityCalculatorTests
{
    [Fact]
    public void Calculate_AllZeroInputs_ReturnsZeroResilient()
    {
        var input = new CoverageFragilityInput(0m, 0m, 0m, 10);
        var result = CoverageFragilityCalculator.Calculate(input);
        result.FragilityScore.Should().Be(0m);
        result.BurnoutPressureComponent.Should().Be(0m);
        result.DependencyConcentrationComponent.Should().Be(0m);
        result.CriticalExposureComponent.Should().Be(0m);
    }

    [Fact]
    public void Calculate_AllMaxInputs_Returns100Critical()
    {
        // HighCriticalBurnoutPct=1, ConcentrationRatio=1, MeanDependency=100
        var input = new CoverageFragilityInput(1m, 1m, 100m, 10);
        var result = CoverageFragilityCalculator.Calculate(input);
        result.FragilityScore.Should().Be(100m);
    }

    [Fact]
    public void Calculate_BurnoutPressureOnly_Returns25Points()
    {
        // 100% at High/Critical burnout → 25 pts, rest 0
        var input = new CoverageFragilityInput(1m, 0m, 0m, 10);
        var result = CoverageFragilityCalculator.Calculate(input);
        result.BurnoutPressureComponent.Should().Be(25m);
        result.DependencyConcentrationComponent.Should().Be(0m);
        result.CriticalExposureComponent.Should().Be(0m);
        result.FragilityScore.Should().Be(25m);
    }

    [Fact]
    public void Calculate_ConcentrationOnly_Returns40Points()
    {
        // All employees with critical-shift ownership → 40 pts
        var input = new CoverageFragilityInput(0m, 1m, 0m, 10);
        var result = CoverageFragilityCalculator.Calculate(input);
        result.DependencyConcentrationComponent.Should().Be(40m);
        result.FragilityScore.Should().Be(40m);
    }

    [Fact]
    public void Calculate_MeanDependency100_Returns35Points()
    {
        var input = new CoverageFragilityInput(0m, 0m, 100m, 10);
        var result = CoverageFragilityCalculator.Calculate(input);
        result.CriticalExposureComponent.Should().Be(35m);
        result.FragilityScore.Should().Be(35m);
    }

    [Fact]
    public void Calculate_HalfInputs_ReturnsHalfScore()
    {
        // 0.5 burnout, 0.5 concentration, 50 mean dep → 12.5 + 20 + 17.5 = 50
        var input = new CoverageFragilityInput(0.5m, 0.5m, 50m, 10);
        var result = CoverageFragilityCalculator.Calculate(input);
        result.FragilityScore.Should().Be(50m);
    }

    [Fact]
    public void Calculate_ScoreBelow25_ClassifiedResilient()
    {
        var input = new CoverageFragilityInput(0.1m, 0m, 0m, 10);
        var result = CoverageFragilityCalculator.Calculate(input);
        result.FragilityScore.Should().BeLessThan(25m);
    }

    [Fact]
    public void Calculate_Score75OrAbove_ClassifiedCritical()
    {
        // 25 + 40 + 35 = 100 → Critical
        var input = new CoverageFragilityInput(1m, 1m, 100m, 10);
        var result = CoverageFragilityCalculator.Calculate(input);
        result.FragilityScore.Should().BeGreaterThanOrEqualTo(75m);
    }

    [Fact]
    public void Calculate_InputsClampedAbove1ForRatios()
    {
        // Ratios > 1 should be clamped to 1 (same as all-max)
        var clamped = CoverageFragilityCalculator.Calculate(new CoverageFragilityInput(2m, 2m, 100m, 10));
        var max = CoverageFragilityCalculator.Calculate(new CoverageFragilityInput(1m, 1m, 100m, 10));
        clamped.FragilityScore.Should().Be(max.FragilityScore);
    }

    [Fact]
    public void Calculate_NullInput_Throws()
    {
        var act = () => CoverageFragilityCalculator.Calculate(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
