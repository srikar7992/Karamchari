// -----------------------------------------------------------------------
// <copyright file="ConfidenceEvaluationEngine.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Intelligence.Domain.Primitives;

namespace Karamchari.Intelligence.Domain.Signals;

/// <summary>
/// Centralized engine for evaluating the confidence of an intelligence signal.
/// Prevents modules from applying subjective or inconsistent weights to evidence.
/// </summary>
public static class ConfidenceEvaluationEngine
{
    /// <summary>
    /// Calculates an aggregate confidence score based on the provided evidence and data freshness.
    /// </summary>
    public static SignalConfidence Evaluate(
        IEnumerable<ContributingFactor> evidence,
        DateTimeOffset lastDataRefreshUtc,
        DateTimeOffset now)
    {
        if (evidence == null || !evidence.Any())
            return SignalConfidence.Create(0, "No Evidence");

        // 1. Base Score calculation based on highest quality evidence
        var baseScore = evidence.Max(e => GetBaseWeight(e.Evidence));

        // 2. Volume bonus (multiple corroborating signals boost confidence)
        var volumeBonus = Math.Min(20m, evidence.Count() * 2m);

        // 3. Temporal Decay
        // Data older than 90 days begins to lose confidence heavily in dynamic environments
        var ageDays = (now - lastDataRefreshUtc).TotalDays;
        var decayPenalty = CalculateDecay(ageDays);

        var finalScore = baseScore + volumeBonus - decayPenalty;

        // Clamp to 0-100
        finalScore = Math.Clamp(finalScore, 0m, 100m);

        var primaryEvidenceType = evidence.OrderByDescending(e => GetBaseWeight(e.Evidence)).First().Evidence.ToString();

        return SignalConfidence.Create(finalScore, primaryEvidenceType);
    }

    private static decimal GetBaseWeight(EvidenceType type) => type switch
    {
        EvidenceType.SystemCalculated => 95m,
        EvidenceType.VerifiedCertificate => 90m,
        EvidenceType.HistoricalPerformance => 80m,
        EvidenceType.ManagerAssessment => 60m,
        EvidenceType.PeerEndorsement => 40m,
        EvidenceType.SelfReported => 20m,
        _ => 10m
    };

    private static decimal CalculateDecay(double ageDays)
    {
        if (ageDays <= 30) return 0m;
        if (ageDays <= 90) return (decimal)((ageDays - 30) * 0.2); // slight decay
        return 12m + (decimal)((ageDays - 90) * 0.5); // rapid decay after a quarter
    }
}
