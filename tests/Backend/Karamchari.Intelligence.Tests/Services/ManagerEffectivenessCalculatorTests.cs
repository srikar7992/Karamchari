// -----------------------------------------------------------------------
// <copyright file="ManagerEffectivenessCalculatorTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class ManagerEffectivenessCalculatorTests
{
    private static ManagerEffectivenessInput Perfect => new(
        ManagerId: Guid.NewGuid(),
        TeamAttendanceRate: 1.0m,
        HighBurnoutFraction: 0m,
        HighAttritionFraction: 0m,
        LeaveDenialRatio: 0m,
        AvgSwapsPerMemberPerMonth: 0m,
        AvgOvertimeHoursPerMember: 10m,
        OrgAvgOvertimeHoursPerEmployee: 10m,
        DirectReportCount: 8
    );

    [Fact]
    public void Calculate_PerfectInputs_Score100()
    {
        var result = ManagerEffectivenessCalculator.Calculate(Perfect);
        result.Should().NotBeNull();
        result!.Score.Should().Be(100m);
    }

    [Fact]
    public void Calculate_BelowMinDirectReports_ReturnsNull()
    {
        var input = Perfect with { DirectReportCount = 4 };
        var result = ManagerEffectivenessCalculator.Calculate(input);
        result.Should().BeNull();
    }

    [Fact]
    public void Calculate_ExactlyMinDirectReports_ReturnsScore()
    {
        var input = Perfect with { DirectReportCount = ManagerEffectivenessCalculator.MinDirectReports };
        var result = ManagerEffectivenessCalculator.Calculate(input);
        result.Should().NotBeNull();
    }

    [Fact]
    public void Calculate_HighBurnoutFraction_LowersBurnoutDimension()
    {
        var input = Perfect with { HighBurnoutFraction = 0.5m };
        var result = ManagerEffectivenessCalculator.Calculate(input)!;
        result.BurnoutDimension.Should().BeApproximately(50m, 0.1m);
    }

    [Fact]
    public void Calculate_HighAttritionFraction_LowersAttritionDimension()
    {
        var input = Perfect with { HighAttritionFraction = 0.4m };
        var result = ManagerEffectivenessCalculator.Calculate(input)!;
        result.AttritionDimension.Should().BeApproximately(60m, 0.1m);
    }

    [Fact]
    public void Calculate_HighLeaveDenialRatio_LowersLeaveHealth()
    {
        var input = Perfect with { LeaveDenialRatio = 0.5m };
        var result = ManagerEffectivenessCalculator.Calculate(input)!;
        result.LeaveHealthDimension.Should().BeApproximately(50m, 0.1m);
    }

    [Fact]
    public void Calculate_TeamOTDoubleOrgAverage_LowersOvertimeDimension()
    {
        var input = Perfect with { AvgOvertimeHoursPerMember = 20m, OrgAvgOvertimeHoursPerEmployee = 10m };
        var result = ManagerEffectivenessCalculator.Calculate(input)!;
        result.OvertimeDimension.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ScoreNeverNegative()
    {
        var worst = Perfect with
        {
            TeamAttendanceRate = 0m,
            HighBurnoutFraction = 1m,
            HighAttritionFraction = 1m,
            LeaveDenialRatio = 1m,
            AvgSwapsPerMemberPerMonth = 10m,
            AvgOvertimeHoursPerMember = 100m,
            OrgAvgOvertimeHoursPerEmployee = 10m
        };
        var result = ManagerEffectivenessCalculator.Calculate(worst)!;
        result.Score.Should().BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void Calculate_ExplanationSummaryNotEmpty()
    {
        var result = ManagerEffectivenessCalculator.Calculate(Perfect)!;
        result.Explanation.Summary.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Calculate_NullInput_Throws()
        => Assert.Throws<ArgumentNullException>(() => ManagerEffectivenessCalculator.Calculate(null!));

    [Fact]
    public void Calculate_ZeroOrgAvgOT_WithNonZeroTeamOT_DoesNotThrow()
    {
        var input = Perfect with { OrgAvgOvertimeHoursPerEmployee = 0m, AvgOvertimeHoursPerMember = 5m };
        var act = () => ManagerEffectivenessCalculator.Calculate(input);
        act.Should().NotThrow();
    }

    [Fact]
    public void Calculate_ZeroOrgAvgOT_ZeroTeamOT_OvertimeDimensionIs100()
    {
        var input = Perfect with { OrgAvgOvertimeHoursPerEmployee = 0m, AvgOvertimeHoursPerMember = 0m };
        var result = ManagerEffectivenessCalculator.Calculate(input)!;
        result.OvertimeDimension.Should().Be(100m);
    }
}
