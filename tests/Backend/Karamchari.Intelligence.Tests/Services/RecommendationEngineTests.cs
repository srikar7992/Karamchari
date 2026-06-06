using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class RecommendationEngineTests
{
    private static BurnoutSignalInput BurnoutInput(int consecutiveDays = 0, decimal otHours = 0, int daysWithoutLeave = 0)
        => new(
            EmployeeId: Guid.NewGuid(),
            ConsecutiveWorkDays: consecutiveDays,
            OvertimeHours28d: otHours,
            DaysWithoutLeave: daysWithoutLeave,
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

    private static AttritionSignalInput AttritionInput(decimal managerFriction = 0m)
        => new(
            EmployeeId: Guid.NewGuid(),
            LateArrivalSlope: 0m,
            LeaveFrequencyRatio: 1m,
            SickLeaveDays30d: 0m,
            HistoricalSickLeaveBaseline: 1m,
            ShiftSwaps30d: 0,
            OvertimeRejections30d: 0,
            PeerAttendanceGap: 0m,
            ManagerFrictionScore: managerFriction,
            DataAgeDays: 120,
            IsNewJoiner: false
        );

    private static ManagerEffectivenessInput ManagerInput(decimal score = 100m)
        => new(
            ManagerId: Guid.NewGuid(),
            TeamAttendanceRate: 1m,
            HighBurnoutFraction: 0m,
            HighAttritionFraction: 0m,
            LeaveDenialRatio: 0m,
            AvgSwapsPerMemberPerMonth: 0m,
            AvgOvertimeHoursPerMember: 10m,
            OrgAvgOvertimeHoursPerEmployee: 10m,
            DirectReportCount: 8
        );

    // ── Burnout recommendations ───────────────────────────────────────────────

    [Fact]
    public void ForBurnout_LowRisk_ReturnsEmpty()
    {
        var result = BurnoutScoreCalculator.Calculate(BurnoutInput());
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Low);

        var recs = RecommendationEngine.ForBurnout(result, BurnoutInput());
        recs.Should().BeEmpty();
    }

    [Fact]
    public void ForBurnout_HighOT_WhenCritical_RecommendReduceOT()
    {
        // Need score ≥ 60 to get High/Critical. Combine multiple signals.
        var input = CriticalBurnoutInput() with { OvertimeHours28d = 45m };
        var result = BurnoutScoreCalculator.Calculate(input);
        result.RiskLevel.Should().BeOneOf(WorkforceRiskLevel.High, WorkforceRiskLevel.Critical);

        var recs = RecommendationEngine.ForBurnout(result, input);
        recs.Should().Contain(r => r.Type == WorkforceRecommendationType.ReduceOvertime);
    }

    [Fact]
    public void ForBurnout_HighLeaveAvoidance_WhenCritical_RecommendScheduleLeave()
    {
        var input = CriticalBurnoutInput() with { DaysWithoutLeave = 200 };
        var result = BurnoutScoreCalculator.Calculate(input);
        result.RiskLevel.Should().BeOneOf(WorkforceRiskLevel.High, WorkforceRiskLevel.Critical);

        var recs = RecommendationEngine.ForBurnout(result, input);
        recs.Should().Contain(r => r.Type == WorkforceRecommendationType.ScheduleLeave);
    }

    [Fact]
    public void ForBurnout_HighConsecutiveDays_WhenCritical_RecommendReduceShifts()
    {
        var input = CriticalBurnoutInput() with { ConsecutiveWorkDays = 9 };
        var result = BurnoutScoreCalculator.Calculate(input);
        result.RiskLevel.Should().BeOneOf(WorkforceRiskLevel.High, WorkforceRiskLevel.Critical);

        var recs = RecommendationEngine.ForBurnout(result, input);
        recs.Should().Contain(r => r.Type == WorkforceRecommendationType.ReduceConsecutiveShifts);
    }

    // CriticalBurnoutInput produces score ≥ 80 when combined with a single extra signal.
    private static BurnoutSignalInput CriticalBurnoutInput() => new(
        EmployeeId: Guid.NewGuid(),
        ConsecutiveWorkDays: 9,
        OvertimeHours28d: 45m,
        DaysWithoutLeave: 200,
        LateArrivalsCurrentMonth: 15,
        LateArrivalsPreviousMonth: 2,
        LateArrivalsMonthMinus2: 0,
        ShiftSwaps30d: 7,
        HighIntensityShiftRatio: 0.8m,
        EmergencyFillIns90d: 12,
        DataAgeDays: 120,
        IsNewJoiner: false,
        ShiftCategory: "DayShift"
    );

    [Fact]
    public void ForBurnout_NullResult_Throws()
        => Assert.Throws<ArgumentNullException>(() => RecommendationEngine.ForBurnout(null!, BurnoutInput()));

    [Fact]
    public void ForBurnout_NullInput_Throws()
    {
        var result = BurnoutScoreCalculator.Calculate(BurnoutInput());
        Assert.Throws<ArgumentNullException>(() => RecommendationEngine.ForBurnout(result, null!));
    }

    // ── Attrition recommendations ─────────────────────────────────────────────

    [Fact]
    public void ForAttrition_LowRisk_ReturnsEmpty()
    {
        var input = AttritionInput();
        var result = AttritionScoreCalculator.Calculate(input);
        result.RiskLevel.Should().Be(WorkforceRiskLevel.Low);

        var recs = RecommendationEngine.ForAttrition(result, input);
        recs.Should().BeEmpty();
    }

    [Fact]
    public void ForAttrition_HighRisk_AlwaysRecommendsManagerCheckIn()
    {
        var input = AttritionInput() with
        {
            LateArrivalSlope = 5m,
            LeaveFrequencyRatio = 4m,
            SickLeaveDays30d = 5m,
            HistoricalSickLeaveBaseline = 1m
        };
        var result = AttritionScoreCalculator.Calculate(input);
        result.RiskLevel.Should().BeOneOf(WorkforceRiskLevel.High, WorkforceRiskLevel.Critical);

        var recs = RecommendationEngine.ForAttrition(result, input);
        recs.Should().Contain(r => r.Type == WorkforceRecommendationType.ManagerCheckIn);
    }

    [Fact]
    public void ForAttrition_HighManagerFriction_RecommendsTeamReassignment()
    {
        var input = AttritionInput() with
        {
            LateArrivalSlope = 5m,
            LeaveFrequencyRatio = 4m,
            SickLeaveDays30d = 5m,
            HistoricalSickLeaveBaseline = 1m,
            ManagerFrictionScore = 0.6m
        };
        var result = AttritionScoreCalculator.Calculate(input);

        var recs = RecommendationEngine.ForAttrition(result, input);
        recs.Should().Contain(r => r.Type == WorkforceRecommendationType.TeamReassignmentReview);
    }

    // ── Manager recommendations ───────────────────────────────────────────────

    [Fact]
    public void ForManager_Score70Plus_ReturnsEmpty()
    {
        var input = ManagerInput();
        var effectivenessResult = ManagerEffectivenessCalculator.Calculate(input)!;
        effectivenessResult.Score.Should().Be(100m);

        var recs = RecommendationEngine.ForManager(effectivenessResult, input);
        recs.Should().BeEmpty();
    }

    [Fact]
    public void ForManager_HighOTDependency_WhenScoreLow_RecommendsReduceOT()
    {
        // Need score < 70 to get recommendations. Combine OT + burnout + attrition.
        var input = ManagerInput() with
        {
            AvgOvertimeHoursPerMember = 30m,  // 3× org avg → OT dimension = 0
            OrgAvgOvertimeHoursPerEmployee = 10m,
            HighBurnoutFraction = 0.6m,       // BurnoutDim = 40
            HighAttritionFraction = 0.5m      // AttritionDim = 50
        };
        var result = ManagerEffectivenessCalculator.Calculate(input)!;
        result.Score.Should().BeLessThan(70m);

        var recs = RecommendationEngine.ForManager(result, input);
        recs.Should().Contain(r => r.Type == WorkforceRecommendationType.ReduceOtReliance);
    }

    [Fact]
    public void ForManager_NullResult_Throws()
        => Assert.Throws<ArgumentNullException>(() => RecommendationEngine.ForManager(null!, ManagerInput()));

    [Fact]
    public void ForManager_NullInput_Throws()
    {
        var result = ManagerEffectivenessCalculator.Calculate(ManagerInput())!;
        Assert.Throws<ArgumentNullException>(() => RecommendationEngine.ForManager(result, null!));
    }
}
