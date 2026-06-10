// -----------------------------------------------------------------------
// <copyright file="CausalChainDetectorTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class CausalChainDetectorTests
{
    private static CausalChainInput AllClear() =>
        new(CriticalShiftCoverageRatio: 0m, OvertimeHours28d: 0m,
            DaysWithoutLeave: 0, ScheduleQualityScore: 100m,
            BurnoutScore: 0m, AttritionScore: 0m);

    [Fact]
    public void Detect_AllClear_ReturnsNoneChain()
    {
        var result = CausalChainDetector.Detect(AllClear());
        result.ActiveLinks.Should().BeEmpty();
        result.ChainSeverity.Should().Be(CausalChainSeverity.None);
    }

    [Fact]
    public void Detect_CoverageShortageThresholdMet_ActiveLinkDetected()
    {
        var input = AllClear() with { CriticalShiftCoverageRatio = 0.50m };
        var result = CausalChainDetector.Detect(input);
        result.ActiveLinks.Should().Contain(CausalChainLink.CoverageShortage);
    }

    [Fact]
    public void Detect_BelowCoverageThreshold_NotActivated()
    {
        var input = AllClear() with { CriticalShiftCoverageRatio = 0.49m };
        var result = CausalChainDetector.Detect(input);
        result.ActiveLinks.Should().NotContain(CausalChainLink.CoverageShortage);
    }

    [Fact]
    public void Detect_OvertimeThresholdMet_OvertimeLinkActive()
    {
        var input = AllClear() with { OvertimeHours28d = 24m };
        var result = CausalChainDetector.Detect(input);
        result.ActiveLinks.Should().Contain(CausalChainLink.OvertimeIncrease);
    }

    [Fact]
    public void Detect_LeaveAvoidanceThresholdMet_LeaveAvoidanceLinkActive()
    {
        var input = AllClear() with { DaysWithoutLeave = 60 };
        var result = CausalChainDetector.Detect(input);
        result.ActiveLinks.Should().Contain(CausalChainLink.LeaveAvoidance);
    }

    [Fact]
    public void Detect_ScheduleQualityBelow60_ScheduleDegradationActive()
    {
        var input = AllClear() with { ScheduleQualityScore = 60m };
        var result = CausalChainDetector.Detect(input);
        result.ActiveLinks.Should().Contain(CausalChainLink.ScheduleDegradation);
    }

    [Fact]
    public void Detect_ScheduleQualityMinus1_ScheduleDegradationSkipped()
    {
        // -1 means data not available — should not fire
        var input = AllClear() with { ScheduleQualityScore = -1m };
        var result = CausalChainDetector.Detect(input);
        result.ActiveLinks.Should().NotContain(CausalChainLink.ScheduleDegradation);
    }

    [Fact]
    public void Detect_BurnoutAbove60_BurnoutRiskActive()
    {
        var input = AllClear() with { BurnoutScore = 60m };
        var result = CausalChainDetector.Detect(input);
        result.ActiveLinks.Should().Contain(CausalChainLink.BurnoutRisk);
    }

    [Fact]
    public void Detect_AttritionAbove60_AttritionRiskActive()
    {
        var input = AllClear() with { AttritionScore = 60m };
        var result = CausalChainDetector.Detect(input);
        result.ActiveLinks.Should().Contain(CausalChainLink.AttritionRisk);
    }

    [Fact]
    public void Detect_OneLink_ReturnsMildSeverity()
    {
        var input = AllClear() with { BurnoutScore = 70m };
        var result = CausalChainDetector.Detect(input);
        result.ChainSeverity.Should().Be(CausalChainSeverity.Mild);
        result.ActiveLinks.Count.Should().Be(1);
    }

    [Fact]
    public void Detect_ThreeLinks_ReturnsModerate()
    {
        var input = AllClear() with
        {
            CriticalShiftCoverageRatio = 0.8m,
            OvertimeHours28d = 30m,
            DaysWithoutLeave = 90
        };
        var result = CausalChainDetector.Detect(input);
        result.ActiveLinks.Count.Should().Be(3);
        result.ChainSeverity.Should().Be(CausalChainSeverity.Moderate);
    }

    [Fact]
    public void Detect_AllSixLinks_ReturnsSevere()
    {
        var input = new CausalChainInput(
            CriticalShiftCoverageRatio: 0.9m,
            OvertimeHours28d: 40m,
            DaysWithoutLeave: 120,
            ScheduleQualityScore: 30m,
            BurnoutScore: 85m,
            AttritionScore: 75m);

        var result = CausalChainDetector.Detect(input);
        result.ActiveLinks.Count.Should().Be(6);
        result.ChainSeverity.Should().Be(CausalChainSeverity.Severe);
    }

    [Fact]
    public void Detect_LinksInCausalOrder()
    {
        var input = new CausalChainInput(
            CriticalShiftCoverageRatio: 0.9m,
            OvertimeHours28d: 40m,
            DaysWithoutLeave: 120,
            ScheduleQualityScore: 30m,
            BurnoutScore: 85m,
            AttritionScore: 75m);

        var result = CausalChainDetector.Detect(input);
        var links = result.ActiveLinks.ToList();
        links[0].Should().Be(CausalChainLink.CoverageShortage);
        links[1].Should().Be(CausalChainLink.OvertimeIncrease);
        links[2].Should().Be(CausalChainLink.LeaveAvoidance);
        links[3].Should().Be(CausalChainLink.ScheduleDegradation);
        links[4].Should().Be(CausalChainLink.BurnoutRisk);
        links[5].Should().Be(CausalChainLink.AttritionRisk);
    }

    [Fact]
    public void Detect_NullInput_Throws()
    {
        var act = () => CausalChainDetector.Detect(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
