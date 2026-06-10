// -----------------------------------------------------------------------
// <copyright file="DefaultOKRScoringStrategy.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Performance.Domain.OKRs;

namespace Karamchari.Performance.Domain.Scoring;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class DefaultOKRScoringStrategy : IOKRScoringStrategy
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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
