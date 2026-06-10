// -----------------------------------------------------------------------
// <copyright file="AttritionScoreCalculatorTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class AttritionScoreCalculatorTests
{
    private static AttritionSignalInput Zero => new(
        EmployeeId: Guid.NewGuid(),
        LateArrivalSlope: 0m,
        LeaveFrequencyRatio: 1.0m,
        SickLeaveDays30d: 0m,
        HistoricalSickLeaveBaseline: 1.0m,
        ShiftSwaps30d: 0,
        OvertimeRejections30d: 0,
        PeerAttendanceGap: 0m,
        ManagerFrictionScore: 0m,
        DataAgeDays: 120,
        IsNewJoiner: false
    );

    // ── AttendanceSlopePoints ─────────────────────────────────────────────────

    [Theory]
    [InlineData(-1.0, 0)]
    [InlineData(0.0, 0)]
    [InlineData(0.5, 5)]
    [InlineData(1.0, 5)]
    [InlineData(1.1, 15)]
    [InlineData(3.0, 15)]
    [InlineData(3.1, 35)]
    [InlineData(10.0, 35)]
    public void AttendanceSlopePoints_MatchThresholds(double slope, decimal expectedPts)
        => AttritionScoreCalculator.AttendanceSlopePoints((decimal)slope).Should().Be(expectedPts);

    // ── LeaveFrequencyPoints ──────────────────────────────────────────────────

    [Theory]
    [InlineData(0.5, 0)]
    [InlineData(1.0, 0)]
    [InlineData(1.1, 10)]
    [InlineData(1.5, 10)]
    [InlineData(1.6, 20)]
    [InlineData(2.5, 20)]
    [InlineData(2.6, 35)]
    [InlineData(5.0, 35)]
    public void LeaveFrequencyPoints_MatchThresholds(double ratio, decimal expectedPts)
        => AttritionScoreCalculator.LeaveFrequencyPoints((decimal)ratio).Should().Be(expectedPts);

    // ── SickLeavePoints ───────────────────────────────────────────────────────

    [Fact]
    public void SickLeavePoints_NoBaseline_WithSickDays_Returns20()
        => AttritionScoreCalculator.SickLeavePoints(3m, 0m).Should().Be(20m);

    [Fact]
    public void SickLeavePoints_NoBaseline_NoSickDays_Returns0()
        => AttritionScoreCalculator.SickLeavePoints(0m, 0m).Should().Be(0m);

    [Theory]
    [InlineData(1.0, 2.0, 0)]    // 0.5× baseline
    [InlineData(1.4, 1.0, 0)]    // 1.4× — below 1.5 threshold
    [InlineData(1.5, 1.0, 10)]   // exactly 1.5×
    [InlineData(1.9, 1.0, 10)]   // 1.9×
    [InlineData(2.0, 1.0, 20)]   // exactly 2×
    [InlineData(2.9, 1.0, 20)]   // 2.9×
    [InlineData(3.0, 1.0, 35)]   // exactly 3×
    [InlineData(5.0, 1.0, 35)]   // 5×
    public void SickLeavePoints_RatioThresholds(double sick, double baseline, decimal expectedPts)
        => AttritionScoreCalculator.SickLeavePoints((decimal)sick, (decimal)baseline).Should().Be(expectedPts);

    // ── ShiftSwapPoints ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 15)]
    [InlineData(5, 15)]
    [InlineData(6, 35)]
    [InlineData(10, 35)]
    public void ShiftSwapPoints_MatchThresholds(int swaps, decimal expectedPts)
        => AttritionScoreCalculator.ShiftSwapPoints(swaps).Should().Be(expectedPts);

    // ── OvertimeRejectionPoints ───────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 15)]
    [InlineData(3, 15)]
    [InlineData(4, 35)]
    [InlineData(10, 35)]
    public void OvertimeRejectionPoints_MatchThresholds(int rejections, decimal expectedPts)
        => AttritionScoreCalculator.OvertimeRejectionPoints(rejections).Should().Be(expectedPts);

    // ── PeerGapPoints ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(5.0, 0)]      // above team
    [InlineData(0.0, 0)]      // at team
    [InlineData(-5.0, 10)]    // slightly below
    [InlineData(-10.0, 10)]   // at -10% threshold
    [InlineData(-15.0, 20)]   // between -10 and -25
    [InlineData(-25.0, 20)]   // at -25% threshold
    [InlineData(-26.0, 35)]   // below -25%
    [InlineData(-50.0, 35)]   // well below
    public void PeerGapPoints_MatchThresholds(double gap, decimal expectedPts)
        => AttritionScoreCalculator.PeerGapPoints((decimal)gap).Should().Be(expectedPts);

    // ── ManagerFrictionPoints ─────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.19, 0)]
    [InlineData(0.2, 15)]
    [InlineData(0.49, 15)]
    [InlineData(0.5, 35)]
    [InlineData(1.0, 35)]
    public void ManagerFrictionPoints_MatchThresholds(double score, decimal expectedPts)
        => AttritionScoreCalculator.ManagerFrictionPoints((decimal)score).Should().Be(expectedPts);

    // ── ScoreToRiskLevel ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, WorkforceRiskLevel.Low)]
    [InlineData(29.9, WorkforceRiskLevel.Low)]
    [InlineData(30, WorkforceRiskLevel.Moderate)]
    [InlineData(59.9, WorkforceRiskLevel.Moderate)]
    [InlineData(60, WorkforceRiskLevel.High)]
    [InlineData(79.9, WorkforceRiskLevel.High)]
    [InlineData(80, WorkforceRiskLevel.Critical)]
    [InlineData(100, WorkforceRiskLevel.Critical)]
    public void ScoreToRiskLevel_MatchBands(double score, WorkforceRiskLevel expected)
        => AttritionScoreCalculator.ScoreToRiskLevel((decimal)score).Should().Be(expected);

    // ── DataAgeToConfidence ───────────────────────────────────────────────────

    [Fact]
    public void DataAgeToConfidence_NewJoiner_ReturnsLow()
        => AttritionScoreCalculator.DataAgeToConfidence(50, true).Should().Be(WorkforceScoreConfidence.Low);

    [Fact]
    public void DataAgeToConfidence_59Days_ReturnsLow()
        => AttritionScoreCalculator.DataAgeToConfidence(59, false).Should().Be(WorkforceScoreConfidence.Low);

    [Fact]
    public void DataAgeToConfidence_60Days_ReturnsMedium()
        => AttritionScoreCalculator.DataAgeToConfidence(60, false).Should().Be(WorkforceScoreConfidence.Medium);

    [Fact]
    public void DataAgeToConfidence_90PlusDays_ReturnsHigh()
        => AttritionScoreCalculator.DataAgeToConfidence(90, false).Should().Be(WorkforceScoreConfidence.High);

    // ── Calculate (integration) ───────────────────────────────────────────────

    [Fact]
    public void Calculate_ZeroInputs_ScoreIsZero()
    {
        var result = AttritionScoreCalculator.Calculate(Zero);
        result.Score.Should().Be(0m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Low);
    }

    [Fact]
    public void Calculate_AllMaxSignals_ScoreIs100()
    {
        var maxInput = Zero with
        {
            LateArrivalSlope = 5m,
            LeaveFrequencyRatio = 4m,
            SickLeaveDays30d = 5m,
            HistoricalSickLeaveBaseline = 1m,
            ShiftSwaps30d = 8,
            OvertimeRejections30d = 5,
            PeerAttendanceGap = -40m,
            ManagerFrictionScore = 0.8m
        };

        var result = AttritionScoreCalculator.Calculate(maxInput);

        result.Score.Should().Be(100m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Critical);
    }

    [Fact]
    public void Calculate_ScoreAlwaysClamped_0To100()
    {
        var result = AttritionScoreCalculator.Calculate(Zero with { LateArrivalSlope = 999m });
        result.Score.Should().BeInRange(0m, 100m);
    }

    [Fact]
    public void Calculate_ExplanationNotNull()
    {
        var result = AttritionScoreCalculator.Calculate(Zero);
        result.Explanation.Should().NotBeNull();
        result.Explanation.Summary.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Calculate_Deterministic()
    {
        var input = Zero with { LeaveFrequencyRatio = 2.0m, SickLeaveDays30d = 3m };
        var r1 = AttritionScoreCalculator.Calculate(input);
        var r2 = AttritionScoreCalculator.Calculate(input);
        r1.Score.Should().Be(r2.Score);
    }

    [Fact]
    public void Calculate_NullInput_Throws()
        => Assert.Throws<ArgumentNullException>(() => AttritionScoreCalculator.Calculate(null!));

    [Fact]
    public void Calculate_HighLeaveSpike_ContributesLeaveFrequencyComponent()
    {
        var input = Zero with { LeaveFrequencyRatio = 3.0m };
        var result = AttritionScoreCalculator.Calculate(input);

        result.ComponentScores["LeaveFrequencyChange"].Should().BePositive();
    }
}
