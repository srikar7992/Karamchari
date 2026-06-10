// -----------------------------------------------------------------------
// <copyright file="TalentRiskCalculatorTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class TalentRiskCalculatorTests
{
    private static TalentRiskInput Input(
        decimal burnout = 0m,
        decimal attrition = 0m,
        decimal dependency = 0m)
        => new(Guid.NewGuid(), burnout, attrition, dependency);

    [Fact]
    public void Calculate_AllZero_ReturnsZeroLowRisk()
    {
        var result = TalentRiskCalculator.Calculate(Input());
        result.Score.Should().Be(0m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Low);
        result.CompoundPenalty.Should().Be(0m);
    }

    [Fact]
    public void Calculate_MaxAllSignals_ReturnsCritical()
    {
        var result = TalentRiskCalculator.Calculate(Input(burnout: 100m, attrition: 100m, dependency: 1m));
        result.Score.Should().Be(100m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Critical);
    }

    [Fact]
    public void Calculate_BurnoutOnly_WeightedCorrectly()
    {
        // 80 burnout × 40% weight = 32 points → Low (< 30 = Low, 30–59 = Moderate)
        var result = TalentRiskCalculator.Calculate(Input(burnout: 80m));
        result.BurnoutComponent.Should().Be(32m);
        result.Score.Should().Be(32m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Moderate);
    }

    [Fact]
    public void Calculate_CompoundPenalty_AppliedWhenBothThresholdsMet()
    {
        // burnout ≥ 70 AND attrition ≥ 60 → compound +15
        var result = TalentRiskCalculator.Calculate(Input(burnout: 75m, attrition: 65m));
        result.CompoundPenalty.Should().Be(15m);
        // base = 75*0.4 + 65*0.4 = 30+26 = 56, +15 = 71
        result.Score.Should().BeApproximately(71m, 0.5m);
    }

    [Fact]
    public void Calculate_CompoundPenalty_NotApplied_WhenOnlyBurnoutHigh()
    {
        var result = TalentRiskCalculator.Calculate(Input(burnout: 75m, attrition: 55m));
        result.CompoundPenalty.Should().Be(0m);
    }

    [Fact]
    public void Calculate_CompoundPenalty_NotApplied_WhenOnlyAttritionHigh()
    {
        var result = TalentRiskCalculator.Calculate(Input(burnout: 60m, attrition: 65m));
        result.CompoundPenalty.Should().Be(0m);
    }

    [Fact]
    public void Calculate_DependencyComponent_WeightedCorrectly()
    {
        // dependency = 0.5 → 50 * 20% = 10 points
        var result = TalentRiskCalculator.Calculate(Input(dependency: 0.5m));
        result.DependencyComponent.Should().BeApproximately(10m, 0.01m);
    }

    [Fact]
    public void Calculate_ScoreClamped_At100()
    {
        var result = TalentRiskCalculator.Calculate(Input(burnout: 100m, attrition: 100m, dependency: 1m));
        result.Score.Should().BeLessOrEqualTo(100m);
    }

    [Fact]
    public void Calculate_NullInput_Throws()
    {
        var act = () => TalentRiskCalculator.Calculate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Calculate_ExplanationContainsContributors()
    {
        var result = TalentRiskCalculator.Calculate(Input(burnout: 60m, attrition: 50m, dependency: 0.3m));
        result.Explanation.Contributors.Should().NotBeEmpty();
        result.Explanation.Summary.Should().Contain("Talent risk");
    }

    [Fact]
    public void ScoreToRiskLevel_Thresholds_Correct()
    {
        TalentRiskCalculator.ScoreToRiskLevel(0m).Should().Be(WorkforceRiskLevel.Low);
        TalentRiskCalculator.ScoreToRiskLevel(29.9m).Should().Be(WorkforceRiskLevel.Low);
        TalentRiskCalculator.ScoreToRiskLevel(30m).Should().Be(WorkforceRiskLevel.Moderate);
        TalentRiskCalculator.ScoreToRiskLevel(59.9m).Should().Be(WorkforceRiskLevel.Moderate);
        TalentRiskCalculator.ScoreToRiskLevel(60m).Should().Be(WorkforceRiskLevel.High);
        TalentRiskCalculator.ScoreToRiskLevel(79.9m).Should().Be(WorkforceRiskLevel.High);
        TalentRiskCalculator.ScoreToRiskLevel(80m).Should().Be(WorkforceRiskLevel.Critical);
    }
}
