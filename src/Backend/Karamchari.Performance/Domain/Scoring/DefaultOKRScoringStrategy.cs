using Karamchari.Performance.Domain.OKRs;

namespace Karamchari.Performance.Domain.Scoring;

public sealed class DefaultOKRScoringStrategy : IOKRScoringStrategy
{
    public decimal ComputeObjectiveScore(IReadOnlyCollection<KeyResult> keyResults)
    {
        ArgumentNullException.ThrowIfNull(keyResults);
        if (keyResults.Count == 0) return 0m;

        var totalWeight = keyResults.Sum(kr => kr.Weight);
        if (totalWeight == 0) return 0m;

        return Math.Round(
            keyResults.Sum(kr => kr.Score * kr.Weight) / totalWeight,
            4);
    }
}
