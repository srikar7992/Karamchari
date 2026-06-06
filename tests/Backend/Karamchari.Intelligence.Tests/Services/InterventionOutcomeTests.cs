using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class InterventionOutcomeTests
{
    private static readonly string TenantId = "t1";
    private static readonly Guid RecId = Guid.NewGuid();
    private static readonly Guid EmpId = Guid.NewGuid();

    [Fact]
    public void Evaluate_ScoreDrop20OrMore_ReturnsEffective()
    {
        var outcome = InterventionOutcome.Evaluate(TenantId, RecId, EmpId, "Burnout",
            scoreAtRecommendation: 80m,
            scoreAtEvaluation: 55m,
            daysAfterRecommendation: 30);

        outcome.Status.Should().Be(InterventionOutcomeStatus.Effective);
        outcome.ScoreDelta.Should().Be(-25m);
    }

    [Fact]
    public void Evaluate_ScoreDrop5To19_ReturnsPartiallyEffective()
    {
        var outcome = InterventionOutcome.Evaluate(TenantId, RecId, EmpId, "Burnout",
            scoreAtRecommendation: 75m,
            scoreAtEvaluation: 65m,
            daysAfterRecommendation: 30);

        outcome.Status.Should().Be(InterventionOutcomeStatus.PartiallyEffective);
        outcome.ScoreDelta.Should().Be(-10m);
    }

    [Fact]
    public void Evaluate_ScoreUnchanged_ReturnsIneffective()
    {
        var outcome = InterventionOutcome.Evaluate(TenantId, RecId, EmpId, "Burnout",
            scoreAtRecommendation: 70m,
            scoreAtEvaluation: 70m,
            daysAfterRecommendation: 30);

        outcome.Status.Should().Be(InterventionOutcomeStatus.Ineffective);
        outcome.ScoreDelta.Should().Be(0m);
    }

    [Fact]
    public void Evaluate_ScoreWorsened_ReturnsIneffective()
    {
        var outcome = InterventionOutcome.Evaluate(TenantId, RecId, EmpId, "Attrition",
            scoreAtRecommendation: 60m,
            scoreAtEvaluation: 78m,
            daysAfterRecommendation: 35);

        outcome.Status.Should().Be(InterventionOutcomeStatus.Ineffective);
        outcome.ScoreDelta.Should().Be(18m);
    }

    [Fact]
    public void Evaluate_DeltaExactlyMinus20_ReturnsEffective()
    {
        var outcome = InterventionOutcome.Evaluate(TenantId, RecId, EmpId, "Burnout",
            scoreAtRecommendation: 80m,
            scoreAtEvaluation: 60m,
            daysAfterRecommendation: 30);

        outcome.Status.Should().Be(InterventionOutcomeStatus.Effective);
    }

    [Fact]
    public void Evaluate_DeltaExactlyMinus5_ReturnsPartiallyEffective()
    {
        var outcome = InterventionOutcome.Evaluate(TenantId, RecId, EmpId, "Burnout",
            scoreAtRecommendation: 75m,
            scoreAtEvaluation: 70m,
            daysAfterRecommendation: 30);

        outcome.Status.Should().Be(InterventionOutcomeStatus.PartiallyEffective);
    }

    [Fact]
    public void Evaluate_PopulatesAllFields()
    {
        var outcome = InterventionOutcome.Evaluate(TenantId, RecId, EmpId, "Attrition",
            scoreAtRecommendation: 72m,
            scoreAtEvaluation: 48m,
            daysAfterRecommendation: 31);

        outcome.TenantId.Should().Be(TenantId);
        outcome.RecommendationId.Should().Be(RecId);
        outcome.EmployeeId.Should().Be(EmpId);
        outcome.ScoreType.Should().Be("Attrition");
        outcome.ScoreAtRecommendation.Should().Be(72m);
        outcome.ScoreAtEvaluation.Should().Be(48m);
        outcome.DaysAfterRecommendation.Should().Be(31);
        outcome.EvaluatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
