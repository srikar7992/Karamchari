// -----------------------------------------------------------------------
// <copyright file="HotspotCalculatorTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class HotspotCalculatorTests
{
    [Fact]
    public void Calculate_EmptyList_ReturnsZeroedNormalResult()
    {
        var result = HotspotCalculator.Calculate(Array.Empty<EmployeeScorePair>());
        result.TotalEmployees.Should().Be(0);
        result.MeanBurnoutScore.Should().Be(0m);
        result.MeanAttritionScore.Should().Be(0m);
        result.BurnoutSeverity.Should().Be(HotspotSeverity.Normal);
        result.AttritionSeverity.Should().Be(HotspotSeverity.Normal);
        result.HighRiskBurnoutCount.Should().Be(0);
        result.CriticalRiskBurnoutCount.Should().Be(0);
    }

    [Fact]
    public void Calculate_AllLowRisk_ReturnsSeverityNormal()
    {
        var pairs = new[]
        {
            new EmployeeScorePair(10m, 15m),
            new EmployeeScorePair(20m, 25m),
            new EmployeeScorePair(15m, 10m)
        };
        var result = HotspotCalculator.Calculate(pairs);
        result.BurnoutSeverity.Should().Be(HotspotSeverity.Normal);
        result.AttritionSeverity.Should().Be(HotspotSeverity.Normal);
        result.HighRiskBurnoutCount.Should().Be(0);
        result.CriticalRiskBurnoutCount.Should().Be(0);
    }

    [Fact]
    public void Calculate_AllCriticalRisk_ReturnsSeverityCritical()
    {
        var pairs = new[]
        {
            new EmployeeScorePair(85m, 82m),
            new EmployeeScorePair(90m, 88m),
            new EmployeeScorePair(82m, 80m)
        };
        var result = HotspotCalculator.Calculate(pairs);
        result.BurnoutSeverity.Should().Be(HotspotSeverity.Critical);
        result.AttritionSeverity.Should().Be(HotspotSeverity.Critical);
        result.CriticalRiskBurnoutCount.Should().Be(3);
        result.CriticalRiskAttritionCount.Should().Be(3);
        result.HighRiskBurnoutCount.Should().Be(0); // all critical, none just "high"
    }

    [Fact]
    public void Calculate_MixedRisk_CountsCorrectly()
    {
        // burnout: 20(low), 65(high), 85(crit) — mean=56.7 → Elevated
        var pairs = new[]
        {
            new EmployeeScorePair(20m, 10m),
            new EmployeeScorePair(65m, 30m),
            new EmployeeScorePair(85m, 50m)
        };
        var result = HotspotCalculator.Calculate(pairs);
        result.TotalEmployees.Should().Be(3);
        result.HighRiskBurnoutCount.Should().Be(1);    // 65 is in [60,80)
        result.CriticalRiskBurnoutCount.Should().Be(1); // 85 is >= 80
        result.BurnoutSeverity.Should().Be(HotspotSeverity.Elevated);
    }

    [Fact]
    public void Calculate_MeanBurnoutExactly65_ReturnsCritical()
    {
        var pairs = new[]
        {
            new EmployeeScorePair(65m, 0m),
            new EmployeeScorePair(65m, 0m)
        };
        var result = HotspotCalculator.Calculate(pairs);
        result.MeanBurnoutScore.Should().Be(65m);
        result.BurnoutSeverity.Should().Be(HotspotSeverity.Critical);
    }

    [Fact]
    public void Calculate_MeanBurnoutExactly40_ReturnsElevated()
    {
        var pairs = new[]
        {
            new EmployeeScorePair(40m, 0m),
            new EmployeeScorePair(40m, 0m)
        };
        var result = HotspotCalculator.Calculate(pairs);
        result.BurnoutSeverity.Should().Be(HotspotSeverity.Elevated);
    }

    [Fact]
    public void Calculate_StdDevIsZero_ForUniformScores()
    {
        var pairs = Enumerable.Repeat(new EmployeeScorePair(50m, 30m), 10).ToList();
        var result = HotspotCalculator.Calculate(pairs);
        result.BurnoutStdDev.Should().Be(0m);
        result.AttritionStdDev.Should().Be(0m);
    }

    [Fact]
    public void Calculate_StdDevPositive_ForDispersedScores()
    {
        var pairs = new[]
        {
            new EmployeeScorePair(10m, 0m),
            new EmployeeScorePair(50m, 0m),
            new EmployeeScorePair(90m, 0m)
        };
        var result = HotspotCalculator.Calculate(pairs);
        result.BurnoutStdDev.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void Calculate_HighCriticalPct_ComputedCorrectly()
    {
        // 2 out of 4 are high or critical burnout
        var pairs = new[]
        {
            new EmployeeScorePair(20m, 0m),
            new EmployeeScorePair(30m, 0m),
            new EmployeeScorePair(65m, 0m),
            new EmployeeScorePair(85m, 0m)
        };
        var hotspot = WorkforceHotspot.Create(
            "t1", "tenant:t1", 4,
            50m, 0m, 10m, 0m,
            highBurnout: 1, criticalBurnout: 1,
            highAttrition: 0, criticalAttrition: 0);

        hotspot.HighCriticalBurnoutPct.Should().Be(50m);
        hotspot.HighCriticalAttritionPct.Should().Be(0m);
    }

    [Fact]
    public void Calculate_NullInput_Throws()
    {
        var act = () => HotspotCalculator.Calculate(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
