// -----------------------------------------------------------------------
// <copyright file="InterventionOutcome.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Effectiveness classification for a closed intervention.
/// </summary>
public enum InterventionOutcomeStatus
{
    /// <summary>Score dropped 20+ points — clear improvement attributable to intervention.</summary>
    Effective,
    /// <summary>Score dropped 5–19 points — partial improvement.</summary>
    PartiallyEffective,
    /// <summary>Score unchanged or worsened after intervention.</summary>
    Ineffective,
    /// <summary>Not enough time has elapsed or follow-up score unavailable.</summary>
    Undetermined
}

/// <summary>
/// How the outcome was observed. Determines the reliability and interpretation of
/// <see cref="InterventionOutcome.Status"/>.
/// </summary>
public enum ObservedOutcomeSource
{
    /// <summary>Outcome derived from the risk score model delta (primary automated path).</summary>
    ScoreModel,
    /// <summary>Outcome confirmed by an employment status change (e.g. resignation = Ineffective).</summary>
    EmploymentStatus,
    /// <summary>Outcome confirmed or updated from an engagement survey response.</summary>
    EngagementSurvey,
    /// <summary>Outcome confirmed or updated from a performance review record.</summary>
    PerformanceReview
}

/// <summary>
/// Closes the loop on a <see cref="WorkforceRecommendation"/>.
/// Created by <c>InterventionTrackerService</c> 30 days after a recommendation is resolved.
/// Maps to Intel_InterventionOutcomes.
/// </summary>
public sealed class InterventionOutcome : ITenantOwned
{
    public Guid Id { get; private set; }

    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>The recommendation this outcome evaluates.</summary>
    public Guid RecommendationId { get; private set; }

    public Guid EmployeeId { get; private set; }

    /// <summary>"Burnout" or "Attrition" — score type the recommendation targeted.</summary>
    public string ScoreType { get; private set; } = string.Empty;

    /// <summary>Score at the time the recommendation was created.</summary>
    public decimal ScoreAtRecommendation { get; private set; }

    /// <summary>Score 30 days after the recommendation was resolved.</summary>
    public decimal ScoreAtEvaluation { get; private set; }

    /// <summary>ScoreAtEvaluation − ScoreAtRecommendation. Negative = improvement.</summary>
    public decimal ScoreDelta { get; private set; }

    public InterventionOutcomeStatus Status { get; private set; }

    /// <summary>How many days after RecommendationCreatedAt the evaluation ran.</summary>
    public int DaysAfterRecommendation { get; private set; }

    public DateTime EvaluatedAt { get; private set; }

    /// <summary>Which data source produced this outcome evaluation.</summary>
    public ObservedOutcomeSource ObservedOutcomeSource { get; private set; }

    private InterventionOutcome() { }

    public static InterventionOutcome Evaluate(
        string tenantId,
        Guid recommendationId,
        Guid employeeId,
        string scoreType,
        decimal scoreAtRecommendation,
        decimal scoreAtEvaluation,
        int daysAfterRecommendation,
        ObservedOutcomeSource observedOutcomeSource = ObservedOutcomeSource.ScoreModel)
    {
        var delta = scoreAtEvaluation - scoreAtRecommendation;
        var status = ClassifyDelta(delta);

        return new InterventionOutcome
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecommendationId = recommendationId,
            EmployeeId = employeeId,
            ScoreType = scoreType,
            ScoreAtRecommendation = scoreAtRecommendation,
            ScoreAtEvaluation = scoreAtEvaluation,
            ScoreDelta = delta,
            Status = status,
            DaysAfterRecommendation = daysAfterRecommendation,
            EvaluatedAt = DateTime.UtcNow,
            ObservedOutcomeSource = observedOutcomeSource
        };
    }

    private static InterventionOutcomeStatus ClassifyDelta(decimal delta) => delta switch
    {
        <= -20m => InterventionOutcomeStatus.Effective,
        <= -5m => InterventionOutcomeStatus.PartiallyEffective,
        _ => InterventionOutcomeStatus.Ineffective
    };
}
