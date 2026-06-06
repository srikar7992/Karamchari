using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class AbsenceContagionCalculatorTests
{
    [Fact]
    public void Calculate_NormalAbsenceRate_ReturnsLowRisk()
    {
        // Current = baseline = 2% → z-score = 0
        var result = AbsenceContagionCalculator.Calculate(
            new AbsenceContagionInput(0.02m, 0.02m, 50));
        result.ZScore.Should().Be(0m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Low);
    }

    [Fact]
    public void Calculate_DoubledAbsenceRate_LargeTeam_ReturnsModerate()
    {
        // baseline = 5%, current = 10%, n = 100 → z = (0.10-0.05)/sqrt(0.05*0.95/100)
        // stdErr ≈ 0.0218 → z ≈ 2.29 → Moderate (1.5–2.5 band)
        var result = AbsenceContagionCalculator.Calculate(
            new AbsenceContagionInput(0.10m, 0.05m, 100));
        result.ZScore.Should().BeApproximately(2.29m, 0.05m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Moderate);
    }

    [Fact]
    public void Calculate_TripleAbsenceRate_LargeTeam_ReturnsHigh()
    {
        // baseline = 5%, current = 15%, n = 100 → z = (0.15-0.05)/0.0218 ≈ 4.59 → Critical
        // Use baseline 8%, current 18%, n=100 → z ≈ (0.10)/sqrt(0.08*0.92/100) ≈ 0.10/0.0271 ≈ 3.69 → High
        var result = AbsenceContagionCalculator.Calculate(
            new AbsenceContagionInput(0.18m, 0.08m, 100));
        result.ZScore.Should().BeGreaterThan(2.5m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.High);
    }

    [Fact]
    public void Calculate_MassiveSpike_ReturnsCritical()
    {
        // baseline = 2%, current = 30%, n = 200 → very high z-score
        var result = AbsenceContagionCalculator.Calculate(
            new AbsenceContagionInput(0.30m, 0.02m, 200));
        result.ZScore.Should().BeGreaterThan(4m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Critical);
    }

    [Fact]
    public void Calculate_SmallTeamZeroBaseline_ReturnsLow()
    {
        // baseline = 0 — can't compute meaningful z-test
        var result = AbsenceContagionCalculator.Calculate(
            new AbsenceContagionInput(0.10m, 0m, 5));
        result.ZScore.Should().Be(0m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Low);
    }

    [Fact]
    public void Calculate_TeamSizeLessThan2_ReturnsLow()
    {
        var result = AbsenceContagionCalculator.Calculate(
            new AbsenceContagionInput(0.50m, 0.05m, 1));
        result.ZScore.Should().Be(0m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Low);
    }

    [Fact]
    public void Calculate_SlightlyElevated_ReturnsModerate()
    {
        // z-score between 1.5 and 2.5 → Moderate
        // baseline = 10%, current = 14%, n = 200
        // stdErr ≈ 0.021 → z ≈ (0.14-0.10)/0.021 ≈ 1.9
        var result = AbsenceContagionCalculator.Calculate(
            new AbsenceContagionInput(0.14m, 0.10m, 200));
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Moderate);
    }

    [Fact]
    public void Calculate_OutputContainsInputRates()
    {
        var result = AbsenceContagionCalculator.Calculate(
            new AbsenceContagionInput(0.08m, 0.04m, 100));
        result.CurrentAbsenceRate.Should().Be(0.08m);
        result.BaselineAbsenceRate.Should().Be(0.04m);
        result.TeamSize.Should().Be(100);
    }

    [Fact]
    public void Calculate_NullInput_Throws()
    {
        var act = () => AbsenceContagionCalculator.Calculate(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
