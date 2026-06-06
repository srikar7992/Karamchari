using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Domain;

public class WorkforceRiskLevelTests
{
    [Fact]
    public void WorkforceBurnoutScore_Create_SetsAllFields()
    {
        var emp = Guid.NewGuid();
        var explanation = BuildExplanation();

        var score = WorkforceBurnoutScore.Create(
            "tenant1", emp,
            score: 72m,
            riskLevel: WorkforceRiskLevel.High,
            confidence: WorkforceScoreConfidence.High,
            explanation: explanation,
            componentScoresJson: "{}");

        score.TenantId.Should().Be("tenant1");
        score.EmployeeId.Should().Be(emp);
        score.Score.Should().Be(72m);
        score.RiskLevel.Should().Be(WorkforceRiskLevel.High);
        score.Confidence.Should().Be(WorkforceScoreConfidence.High);
        score.Id.Should().NotBe(Guid.Empty);
        score.CalculatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void WorkforceBurnoutScore_Refresh_UpdatesScore()
    {
        var score = WorkforceBurnoutScore.Create("t", Guid.NewGuid(), 50m,
            WorkforceRiskLevel.Moderate, WorkforceScoreConfidence.Medium,
            BuildExplanation(), "{}");

        score.Refresh(85m, WorkforceRiskLevel.Critical, WorkforceScoreConfidence.High,
            BuildExplanation(), "{}");

        score.Score.Should().Be(85m);
        score.RiskLevel.Should().Be(WorkforceRiskLevel.Critical);
    }

    [Fact]
    public void WorkforceAttritionScore_Create_SetsAllFields()
    {
        var emp = Guid.NewGuid();
        var attrition = WorkforceAttritionScore.Create(
            "tenant2", emp, 30m,
            WorkforceRiskLevel.Moderate, WorkforceScoreConfidence.Medium,
            BuildExplanation(), "{}");

        attrition.Score.Should().Be(30m);
        attrition.RiskLevel.Should().Be(WorkforceRiskLevel.Moderate);
    }

    [Fact]
    public void WorkforceRecommendation_Raise_SetsOpenState()
    {
        var emp = Guid.NewGuid();
        var rec = WorkforceRecommendation.Raise(
            "tenant", emp,
            WorkforceRecommendationType.ScheduleLeave,
            RecommendationPriority.High,
            "No leave taken in 200 days",
            80m);

        rec.ResolvedAt.Should().BeNull();
        rec.Type.Should().Be(WorkforceRecommendationType.ScheduleLeave);
        rec.TriggerScore.Should().Be(80m);
    }

    [Fact]
    public void WorkforceRecommendation_Resolve_SetsResolvedAt()
    {
        var rec = WorkforceRecommendation.Raise("t", Guid.NewGuid(),
            WorkforceRecommendationType.ReduceOvertime, RecommendationPriority.Medium,
            "High OT detected", 70m);

        rec.Resolve("manager@corp.com");

        rec.ResolvedAt.Should().HaveValue();
        rec.ResolvedBy.Should().Be("manager@corp.com");
    }

    [Fact]
    public void WorkforceRecommendation_Resolve_IsIdempotent()
    {
        var rec = WorkforceRecommendation.Raise("t", Guid.NewGuid(),
            WorkforceRecommendationType.ReduceOvertime, RecommendationPriority.Medium,
            "OT issue", 60m);

        rec.Resolve("user1");
        var firstResolvedAt = rec.ResolvedAt;

        rec.Resolve("user2"); // second call — should not overwrite
        rec.ResolvedAt.Should().Be(firstResolvedAt);
        rec.ResolvedBy.Should().Be("user1");
    }

    [Fact]
    public void WorkforceRecommendation_Raise_EmptyRationale_Throws()
    {
        var act = () => WorkforceRecommendation.Raise("t", Guid.NewGuid(),
            WorkforceRecommendationType.ScheduleLeave, RecommendationPriority.Low, "", 50m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WorkforceSignalRecord_Record_SetsAllFields()
    {
        var emp = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var record = WorkforceSignalRecord.Record(
            "tenant", emp, WorkforceSignalType.OvertimeHours28d, 42.5m, today);

        record.TenantId.Should().Be("tenant");
        record.EmployeeId.Should().Be(emp);
        record.SignalType.Should().Be(WorkforceSignalType.OvertimeHours28d);
        record.Value.Should().Be(42.5m);
        record.SignalDate.Should().Be(today);
        record.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void WorkforceRiskLevel_CriticalIsHighestValue()
    {
        var all = Enum.GetValues<WorkforceRiskLevel>();
        all.Max().Should().Be(WorkforceRiskLevel.Critical);
    }

    [Fact]
    public void WorkforceScoreConfidence_HighIsHighestValue()
    {
        var all = Enum.GetValues<WorkforceScoreConfidence>();
        all.Max().Should().Be(WorkforceScoreConfidence.High);
    }

    private static Karamchari.Intelligence.Domain.Signals.ScoreExplanation BuildExplanation()
        => Karamchari.Intelligence.Domain.Signals.ScoreExplanation.Compile([], [], [], "Test summary.");
}
