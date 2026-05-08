using Karamchari.Performance.Domain.Reviews;

namespace Karamchari.Performance.Domain.Scoring;

/// <summary>
/// Computes a ReviewSubmission's composite score from its responses.
/// Default strategy: section-weighted average → submission score.
/// Final score across reviewers is computed separately by ReviewCycleService.
/// </summary>
public interface IReviewScoringStrategy
{
    decimal ComputeSubmissionScore(
        IReadOnlyCollection<ReviewResponse> responses,
        ReviewTemplate reviewTemplate);
}
