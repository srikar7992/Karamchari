using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class DependencyRiskCalculatorTests
{
    [Fact]
    public void Calculate_AllZeroRiskInputs_ReturnsZeroScore()
    {
        // depth=5 (saturation) means team is well cross-covered → zero inverse-training risk
        var input = new DependencyRiskInput(0m, 5, 0, 0);
        var result = DependencyRiskCalculator.Calculate(input);
        result.Score.Should().Be(0m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Low);
    }

    [Fact]
    public void Calculate_NoCrossTraining_Contributes25Points()
    {
        // CrossTrainingDepth=0 → max inverse-training risk (25 pts), all else zero → score=25 Low
        var input = new DependencyRiskInput(0m, 0, 0, 0);
        var result = DependencyRiskCalculator.Calculate(input);
        result.CrossTrainingComponent.Should().Be(25m);
        result.Score.Should().Be(25m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Low);
    }

    [Fact]
    public void Calculate_MaxAllInputs_Returns100Critical()
    {
        // ratio=1, depth=0 (no cross-training), unique=1, authority=3
        var input = new DependencyRiskInput(1m, 0, 1, 3);
        var result = DependencyRiskCalculator.Calculate(input);
        result.Score.Should().Be(100m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Critical);
    }

    [Fact]
    public void Calculate_CriticalShiftOnlyMax_ReturnsFortyPoints()
    {
        // Only critical shift ownership at 100% — contributes 40 points
        var input = new DependencyRiskInput(1m, 5, 0, 0);
        var result = DependencyRiskCalculator.Calculate(input);
        // CriticalShift=40, CrossTrain=0 (depth≥5), UniqueRole=0, Approval=0
        result.CriticalShiftComponent.Should().Be(40m);
        result.CrossTrainingComponent.Should().Be(0m);
        result.Score.Should().Be(40m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Moderate);
    }

    [Fact]
    public void Calculate_UniqueRoleAndNoOtherRisk_Returns25Points()
    {
        var input = new DependencyRiskInput(0m, 5, 1, 0);
        var result = DependencyRiskCalculator.Calculate(input);
        result.UniqueRoleComponent.Should().Be(25m);
        result.Score.Should().Be(25m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Low);
    }

    [Fact]
    public void Calculate_CrossTrainingDepthZero_MaximumInverseContribution()
    {
        // depth=0 → full inverse cross-training risk (25 pts)
        var input = new DependencyRiskInput(0m, 0, 0, 0);
        var result = DependencyRiskCalculator.Calculate(input);
        result.CrossTrainingComponent.Should().Be(25m);
    }

    [Fact]
    public void Calculate_CrossTrainingDepthFive_ZeroInverseContribution()
    {
        // depth=5 (saturation) → zero cross-training risk
        var input = new DependencyRiskInput(0m, 5, 0, 0);
        var result = DependencyRiskCalculator.Calculate(input);
        result.CrossTrainingComponent.Should().Be(0m);
    }

    [Fact]
    public void Calculate_CrossTrainingDepthAboveSaturation_Clamped()
    {
        // depth=10 should not produce negative contribution
        var input = new DependencyRiskInput(0m, 10, 0, 0);
        var result = DependencyRiskCalculator.Calculate(input);
        result.CrossTrainingComponent.Should().Be(0m);
        result.Score.Should().BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void Calculate_ModerateAuthority_CorrectApprovalComponent()
    {
        // ApprovalIndex=2 → 2/3 * 100 * 0.10 ≈ 6.7 → rounded
        var input = new DependencyRiskInput(0m, 5, 0, 2);
        var result = DependencyRiskCalculator.Calculate(input);
        result.ApprovalComponent.Should().BeApproximately(6.7m, 0.1m);
    }

    [Fact]
    public void Calculate_ScoreBoundary80_ReturnsHigh()
    {
        // Craft input that produces score just below 80
        // CriticalShift=0.75*40=30, CrossTrain(depth=2)=(5-2)/5*100*0.25=15, unique=25, approval=0 → 70
        // Need to get to ~79: CriticalShift=1→40, CrossTrain=0→25, unique=0→0, approval=3→10 → 75 (High)
        var input = new DependencyRiskInput(1m, 0, 0, 3);
        var result = DependencyRiskCalculator.Calculate(input);
        result.Score.Should().Be(75m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.High);
    }

    [Fact]
    public void Calculate_NullInput_Throws()
    {
        var act = () => DependencyRiskCalculator.Calculate(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
