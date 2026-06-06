using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class BurnoutScoreCalculatorTests
{
    private static BurnoutSignalInput Zero => new(
        EmployeeId: Guid.NewGuid(),
        ConsecutiveWorkDays: 0,
        OvertimeHours28d: 0m,
        DaysWithoutLeave: 0,
        LateArrivalsCurrentMonth: 0,
        LateArrivalsPreviousMonth: 0,
        LateArrivalsMonthMinus2: 0,
        ShiftSwaps30d: 0,
        HighIntensityShiftRatio: 0m,
        EmergencyFillIns90d: 0,
        DataAgeDays: 120,
        IsNewJoiner: false,
        ShiftCategory: "DayShift"
    );

    // ── ConsecutiveDayPoints ──────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 0)]
    [InlineData(6, 5)]
    [InlineData(7, 10)]
    [InlineData(8, 20)]
    [InlineData(9, 35)]
    [InlineData(15, 35)]
    public void ConsecutiveDayPoints_MatchThresholds(int days, decimal expectedPts)
        => BurnoutScoreCalculator.ConsecutiveDayPoints(days).Should().Be(expectedPts);

    // ── OvertimePoints ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0)]
    [InlineData(9.9, 0)]
    [InlineData(10, 10)]
    [InlineData(19.9, 10)]
    [InlineData(20, 20)]
    [InlineData(39.9, 20)]
    [InlineData(40, 35)]
    [InlineData(55, 35)]
    public void OvertimePoints_MatchThresholds(double hours, decimal expectedPts)
        => BurnoutScoreCalculator.OvertimePoints((decimal)hours).Should().Be(expectedPts);

    // ── LeaveAvoidancePoints ──────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0)]
    [InlineData(89, 0)]
    [InlineData(90, 10)]
    [InlineData(179, 10)]
    [InlineData(180, 20)]
    [InlineData(269, 20)]
    [InlineData(270, 35)]
    [InlineData(500, 35)]
    public void LeaveAvoidancePoints_MatchThresholds(int days, decimal expectedPts)
        => BurnoutScoreCalculator.LeaveAvoidancePoints(days).Should().Be(expectedPts);

    // ── AttendanceDegradationPoints ───────────────────────────────────────────

    [Theory]
    [InlineData(3, 3, 3, 0)]      // stable
    [InlineData(3, 2, 1, 0)]      // improving
    [InlineData(1, 1, 2, 5)]      // slight increase (+1)
    [InlineData(1, 3, 5, 10)]     // moderate increase (+4)
    [InlineData(1, 5, 9, 20)]     // large increase (+8)
    [InlineData(1, 5, 20, 35)]    // severe increase (+19)
    public void AttendanceDegradationPoints_MatchThresholds(
        int m2, int m1, int current, decimal expectedPts)
        => BurnoutScoreCalculator.AttendanceDegradationPoints(m2, m1, current).Should().Be(expectedPts);

    [Fact]
    public void AttendanceDegradationPoints_SteadyZero_Returns0()
        => BurnoutScoreCalculator.AttendanceDegradationPoints(0, 0, 0).Should().Be(0m);

    // ── ShiftVolatilityPoints ─────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 10)]
    [InlineData(3, 10)]
    [InlineData(4, 20)]
    [InlineData(6, 20)]
    [InlineData(7, 35)]
    public void ShiftVolatilityPoints_MatchThresholds(int swaps, decimal expectedPts)
        => BurnoutScoreCalculator.ShiftVolatilityPoints(swaps).Should().Be(expectedPts);

    // ── HighIntensityPoints ───────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.19, 0)]
    [InlineData(0.2, 10)]
    [InlineData(0.39, 10)]
    [InlineData(0.4, 20)]
    [InlineData(0.69, 20)]
    [InlineData(0.7, 35)]
    [InlineData(1.0, 35)]
    public void HighIntensityPoints_MatchThresholds(double ratio, decimal expectedPts)
        => BurnoutScoreCalculator.HighIntensityPoints((decimal)ratio).Should().Be(expectedPts);

    // ── CoveragePressurePoints ────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 10)]
    [InlineData(5, 10)]
    [InlineData(6, 20)]
    [InlineData(9, 20)]
    [InlineData(10, 35)]
    [InlineData(20, 35)]
    public void CoveragePressurePoints_MatchThresholds(int fillIns, decimal expectedPts)
        => BurnoutScoreCalculator.CoveragePressurePoints(fillIns).Should().Be(expectedPts);

    // ── NormalisePts ──────────────────────────────────────────────────────────

    [Fact]
    public void NormalisePts_MaxPoints_Returns1()
        => BurnoutScoreCalculator.NormalisePts(35m).Should().Be(1m);

    [Fact]
    public void NormalisePts_Zero_Returns0()
        => BurnoutScoreCalculator.NormalisePts(0m).Should().Be(0m);

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
        => BurnoutScoreCalculator.ScoreToRiskLevel((decimal)score).Should().Be(expected);

    // ── DataAgeToConfidence ───────────────────────────────────────────────────

    [Fact]
    public void DataAgeToConfidence_NewJoiner_ReturnsLow()
        => BurnoutScoreCalculator.DataAgeToConfidence(30, true).Should().Be(WorkforceScoreConfidence.Low);

    [Fact]
    public void DataAgeToConfidence_Under60Days_ReturnsLow()
        => BurnoutScoreCalculator.DataAgeToConfidence(59, false).Should().Be(WorkforceScoreConfidence.Low);

    [Fact]
    public void DataAgeToConfidence_60To89Days_ReturnsMedium()
        => BurnoutScoreCalculator.DataAgeToConfidence(75, false).Should().Be(WorkforceScoreConfidence.Medium);

    [Fact]
    public void DataAgeToConfidence_90PlusDays_ReturnsHigh()
        => BurnoutScoreCalculator.DataAgeToConfidence(180, false).Should().Be(WorkforceScoreConfidence.High);

    // ── Calculate (integration) ───────────────────────────────────────────────

    [Fact]
    public void Calculate_ZeroInputs_ScoreIsZero_RiskIsLow()
    {
        var result = BurnoutScoreCalculator.Calculate(Zero);

        result.Score.Should().Be(0m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Low);
        result.ComponentScores.Values.Sum().Should().Be(0m);
    }

    [Fact]
    public void Calculate_AllMaxSignals_ScoreIs100_RiskIsCritical()
    {
        var maxInput = Zero with
        {
            ConsecutiveWorkDays = 10,
            OvertimeHours28d = 50m,
            DaysWithoutLeave = 300,
            LateArrivalsCurrentMonth = 20,
            LateArrivalsPreviousMonth = 1,
            LateArrivalsMonthMinus2 = 0,
            ShiftSwaps30d = 8,
            HighIntensityShiftRatio = 0.9m,
            EmergencyFillIns90d = 12
        };

        var result = BurnoutScoreCalculator.Calculate(maxInput);

        result.Score.Should().Be(100m);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Critical);
    }

    [Fact]
    public void Calculate_ScoreAlwaysClamped_0To100()
    {
        var result = BurnoutScoreCalculator.Calculate(Zero with { ConsecutiveWorkDays = 100 });
        result.Score.Should().BeInRange(0m, 100m);
    }

    [Fact]
    public void Calculate_ExplanationNotNull()
    {
        var result = BurnoutScoreCalculator.Calculate(Zero);
        result.Explanation.Should().NotBeNull();
        result.Explanation.Summary.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Calculate_ComponentScoresMatchWeights()
    {
        // Single high-signal: consecutive days only (9+ days → 35 pts → 100% of that signal)
        var input = Zero with { ConsecutiveWorkDays = 9 };
        var result = BurnoutScoreCalculator.Calculate(input);

        // ConsecutiveWorkDays weight = 25% → contributes 25% * (35/35) * 100 = 25 to score
        result.ComponentScores["ConsecutiveWorkDays"].Should().BeApproximately(25m, 0.1m);
    }

    [Fact]
    public void Calculate_NewJoiner_ConfidenceIsLow()
    {
        var input = Zero with { IsNewJoiner = true, DataAgeDays = 30 };
        var result = BurnoutScoreCalculator.Calculate(input);
        result.Confidence.Should().Be(WorkforceScoreConfidence.Low);
    }

    [Fact]
    public void Calculate_NullInput_Throws()
        => Assert.Throws<ArgumentNullException>(() => BurnoutScoreCalculator.Calculate(null!));

    [Fact]
    public void Calculate_Deterministic_SameInputProducesSameOutput()
    {
        var input = Zero with { ConsecutiveWorkDays = 7, OvertimeHours28d = 25m };
        var r1 = BurnoutScoreCalculator.Calculate(input);
        var r2 = BurnoutScoreCalculator.Calculate(input);
        r1.Score.Should().Be(r2.Score);
        r1.RiskLevel.Should().Be(r2.RiskLevel);
    }
}
