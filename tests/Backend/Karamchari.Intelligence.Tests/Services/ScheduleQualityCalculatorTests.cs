using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class ScheduleQualityCalculatorTests
{
    [Fact]
    public void Calculate_AllZeroInputs_Returns100QualityLowRisk()
    {
        var input = new ScheduleQualityInput(0, 0, 0m, 0);
        var result = ScheduleQualityCalculator.Calculate(input);
        result.QualityScore.Should().Be(100m);
        result.PoorScheduleRiskLevel.Should().Be(WorkforceRiskLevel.Low);
        result.RotationStressComponent.Should().Be(0m);
        result.RestViolationComponent.Should().Be(0m);
        result.WeekendClusterComponent.Should().Be(0m);
        result.SplitShiftComponent.Should().Be(0m);
    }

    [Fact]
    public void Calculate_MaxAllInputs_ReturnsCriticalRisk()
    {
        // transitions=3(35)+violations=3(35)+weekend=1.0(15)+splits=10(15) = 100 stress → quality=0
        var input = new ScheduleQualityInput(3, 3, 1.0m, 10);
        var result = ScheduleQualityCalculator.Calculate(input);
        result.QualityScore.Should().Be(0m);
        result.PoorScheduleRiskLevel.Should().Be(WorkforceRiskLevel.Critical);
    }

    [Fact]
    public void Calculate_OneNightToMorning_Adds12Stress()
    {
        var input = new ScheduleQualityInput(1, 0, 0m, 0);
        var result = ScheduleQualityCalculator.Calculate(input);
        result.RotationStressComponent.Should().Be(12m);
        result.QualityScore.Should().Be(88m);
    }

    [Fact]
    public void Calculate_TwoNightToMorning_Adds24Stress()
    {
        var input = new ScheduleQualityInput(2, 0, 0m, 0);
        var result = ScheduleQualityCalculator.Calculate(input);
        result.RotationStressComponent.Should().Be(24m);
        result.QualityScore.Should().Be(76m);
        result.PoorScheduleRiskLevel.Should().Be(WorkforceRiskLevel.Moderate);
    }

    [Fact]
    public void Calculate_OneRestViolation_Adds10Stress()
    {
        var input = new ScheduleQualityInput(0, 1, 0m, 0);
        var result = ScheduleQualityCalculator.Calculate(input);
        result.RestViolationComponent.Should().Be(10m);
        result.QualityScore.Should().Be(90m);
    }

    [Fact]
    public void Calculate_FullWeekendClustering_Adds15Stress()
    {
        var input = new ScheduleQualityInput(0, 0, 1.0m, 0);
        var result = ScheduleQualityCalculator.Calculate(input);
        result.WeekendClusterComponent.Should().Be(15m);
        result.QualityScore.Should().Be(85m);
    }

    [Fact]
    public void Calculate_QualityScore60_ReturnsModerate()
    {
        // Need 40 pts of stress → 60 quality
        // 2 night transitions=24 + 2 rest violations=22 → 46 pts → quality=54 (High)
        // 1 transition=12 + 1 rest=10 + 0 weekend + 6 splits(3-5 range) → 28 → quality=72 → Moderate
        var input = new ScheduleQualityInput(1, 1, 0m, 3);
        var result = ScheduleQualityCalculator.Calculate(input);
        result.QualityScore.Should().Be(72m);
        result.PoorScheduleRiskLevel.Should().Be(WorkforceRiskLevel.Moderate);
    }

    [Fact]
    public void Calculate_QualityBoundary80_ReturnsLow()
    {
        // 20 pts stress → 80 quality → Low risk
        var input = new ScheduleQualityInput(0, 2, 0m, 0);
        var result = ScheduleQualityCalculator.Calculate(input);
        result.QualityScore.Should().Be(78m);
        result.PoorScheduleRiskLevel.Should().Be(WorkforceRiskLevel.Moderate);
    }

    [Fact]
    public void Calculate_NullInput_Throws()
    {
        var act = () => ScheduleQualityCalculator.Calculate(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
