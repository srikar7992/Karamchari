using Karamchari.Performance.Domain.OKRs;

namespace Karamchari.Performance.Domain.Scoring;

/// <summary>
/// Computes an Objective's aggregated score from its KeyResults.
/// Default strategy: Sum(KR.Score Ã— KR.Weight) / 100.
/// Inject alternative strategies per tenant config if needed.
/// </summary>
public interface IOKRScoringStrategy
{
    decimal ComputeObjectiveScore(IReadOnlyCollection<KeyResult> keyResults);
}
