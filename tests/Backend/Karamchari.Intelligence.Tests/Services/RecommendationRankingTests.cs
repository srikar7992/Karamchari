using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class RecommendationRankingTests
{
    private static BurnoutScoreResult HighRiskResult() =>
        new(
            Score: 75m,
            RiskLevel: WorkforceRiskLevel.High,
            Confidence: WorkforceScoreConfidence.High,
            Explanation: null!,
            ComponentScores: new Dictionary<string, decimal>());

    private static BurnoutSignalInput TriggerAllBurnout() =>
        new(
            EmployeeId: Guid.NewGuid(),
            ConsecutiveWorkDays: 7,
            OvertimeHours28d: 25m,
            DaysWithoutLeave: 100,
            LateArrivalsCurrentMonth: 0,
            LateArrivalsPreviousMonth: 0,
            LateArrivalsMonthMinus2: 0,
            ShiftSwaps30d: 0,
            HighIntensityShiftRatio: 0m,
            EmergencyFillIns90d: 0,
            DataAgeDays: 180,
            IsNewJoiner: false,
            ShiftCategory: "DayShift");

    [Fact]
    public void ForBurnout_WithNullEffectivenessRates_ReturnsRecsInDefaultOrder()
    {
        var result = RecommendationEngine.ForBurnout(HighRiskResult(), TriggerAllBurnout(), null);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void ForBurnout_WithEffectivenessRates_RanksHighestFirst()
    {
        var rates = new Dictionary<WorkforceRecommendationType, decimal>
        {
            [WorkforceRecommendationType.ScheduleLeave] = 0.74m,
            [WorkforceRecommendationType.ReduceOvertime] = 0.45m,
            [WorkforceRecommendationType.ReduceConsecutiveShifts] = 0.61m
        };

        var recs = RecommendationEngine.ForBurnout(HighRiskResult(), TriggerAllBurnout(), rates);

        // ScheduleLeave (0.74) should appear before ReduceConsecutiveShifts (0.61)
        // which should appear before ReduceOvertime (0.45)
        var types = recs.Select(r => r.Type).ToList();
        types[0].Should().Be(WorkforceRecommendationType.ScheduleLeave);
        types[1].Should().Be(WorkforceRecommendationType.ReduceConsecutiveShifts);
        types[2].Should().Be(WorkforceRecommendationType.ReduceOvertime);
    }

    [Fact]
    public void ForBurnout_WithSingleRec_NoRankingNeeded_ReturnsIt()
    {
        var input = TriggerAllBurnout() with
        {
            OvertimeHours28d = 5m,
            DaysWithoutLeave = 10
        };

        var rates = new Dictionary<WorkforceRecommendationType, decimal>
        {
            [WorkforceRecommendationType.ReduceConsecutiveShifts] = 0.9m
        };

        var recs = RecommendationEngine.ForBurnout(HighRiskResult(), input, rates);
        recs.Should().HaveCount(1);
        recs[0].Type.Should().Be(WorkforceRecommendationType.ReduceConsecutiveShifts);
    }

    [Fact]
    public void ForBurnout_LowRisk_ReturnsEmpty_RegardlessOfRates()
    {
        var lowRisk = new BurnoutScoreResult(
            Score: 10m,
            RiskLevel: WorkforceRiskLevel.Low,
            Confidence: WorkforceScoreConfidence.High,
            Explanation: null!,
            ComponentScores: new Dictionary<string, decimal>());

        var rates = new Dictionary<WorkforceRecommendationType, decimal>
        {
            [WorkforceRecommendationType.ReduceOvertime] = 0.99m
        };

        var recs = RecommendationEngine.ForBurnout(lowRisk, TriggerAllBurnout(), rates);
        recs.Should().BeEmpty();
    }

    [Fact]
    public void ForBurnout_EmptyRates_ReturnsRecsInDefaultOrder()
    {
        var empty = new Dictionary<WorkforceRecommendationType, decimal>();
        var recs = RecommendationEngine.ForBurnout(HighRiskResult(), TriggerAllBurnout(), empty);
        recs.Should().NotBeEmpty();
    }
}
