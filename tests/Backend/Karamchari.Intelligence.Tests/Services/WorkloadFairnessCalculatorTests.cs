// -----------------------------------------------------------------------
// <copyright file="WorkloadFairnessCalculatorTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class WorkloadFairnessCalculatorTests
{
    [Fact]
    public void Calculate_EmptyTeam_ReturnsMaxFairness()
    {
        var result = WorkloadFairnessCalculator.Calculate(new WorkloadFairnessInput([]));
        result.OverallScore.Should().Be(100m);
        result.OtGiniCoefficient.Should().Be(0m);
        result.TeamSize.Should().Be(0);
    }

    [Fact]
    public void Calculate_PerfectEquality_ReturnsScore100()
    {
        // All team members have identical OT hours
        var result = WorkloadFairnessCalculator.Calculate(
            new WorkloadFairnessInput([10m, 10m, 10m, 10m]));
        result.OtGiniCoefficient.Should().Be(0m);
        result.OverallScore.Should().Be(100m);
    }

    [Fact]
    public void Calculate_AllOtOnOnePerson_ReturnsLowScore()
    {
        // Worst case: 1 person 100h, 3 people 0h
        var result = WorkloadFairnessCalculator.Calculate(
            new WorkloadFairnessInput([0m, 0m, 0m, 100m]));
        result.OtGiniCoefficient.Should().BeGreaterThan(0.5m);
        result.OverallScore.Should().BeLessThan(50m);
    }

    [Fact]
    public void Calculate_GiniCoefficient_Bounded()
    {
        var result = WorkloadFairnessCalculator.Calculate(
            new WorkloadFairnessInput([0m, 10m, 20m, 80m]));
        result.OtGiniCoefficient.Should().BeInRange(0m, 1m);
        result.OverallScore.Should().BeInRange(0m, 100m);
    }

    [Fact]
    public void Calculate_TeamSize_ReflectsMemberCount()
    {
        var result = WorkloadFairnessCalculator.Calculate(
            new WorkloadFairnessInput([5m, 10m, 15m]));
        result.TeamSize.Should().Be(3);
    }

    [Fact]
    public void Calculate_AvgOtHours_Correct()
    {
        var result = WorkloadFairnessCalculator.Calculate(
            new WorkloadFairnessInput([10m, 20m, 30m]));
        result.AvgOtHoursPerMember.Should().BeApproximately(20m, 0.01m);
    }

    [Fact]
    public void Calculate_MaxOtHours_IsHighest()
    {
        var result = WorkloadFairnessCalculator.Calculate(
            new WorkloadFairnessInput([5m, 25m, 15m]));
        result.MaxOtHoursAnyMember.Should().Be(25m);
    }

    [Fact]
    public void Calculate_Top10Pct_Concentration_ExtremeCase()
    {
        // 10 people; top 1 (10%) has 90h, rest have 10h each
        var members = new List<decimal> { 90m };
        for (int i = 0; i < 9; i++) members.Add(10m);
        var result = WorkloadFairnessCalculator.Calculate(new WorkloadFairnessInput(members));
        // top 10% = 1 person with 90h; total = 90+90 = 180h → 90/180 = 50%
        result.OtConcentrationTop10Pct.Should().BeApproximately(0.5m, 0.05m);
    }

    [Fact]
    public void Calculate_AllZeroOt_ReturnsScore100()
    {
        var result = WorkloadFairnessCalculator.Calculate(
            new WorkloadFairnessInput([0m, 0m, 0m]));
        result.OverallScore.Should().Be(100m);
        result.OtGiniCoefficient.Should().Be(0m);
    }

    [Fact]
    public void Calculate_NullInput_Throws()
    {
        var act = () => WorkloadFairnessCalculator.Calculate(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
