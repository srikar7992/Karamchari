// -----------------------------------------------------------------------
// <copyright file="ConfidenceEvaluationEngineTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Intelligence.Domain.Primitives;
using Karamchari.Intelligence.Domain.Signals;
using Xunit;

namespace Karamchari.Intelligence.Tests.Domain;

public class ConfidenceEvaluationEngineTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static ContributingFactor Factor(EvidenceType type, string name = "Factor")
        => new(name, 1.0m, type, null);

    [Fact]
    public void Engine_SystemCalculated_BaseScoreIs95()
    {
        // Single SystemCalculated factor, fresh data (no decay), no volume bonus beyond 1 * 2 = 2
        var evidence = new[] { Factor(EvidenceType.SystemCalculated) };
        var result = ConfidenceEvaluationEngine.Evaluate(evidence, Now, Now);

        // base=95, volumeBonus=min(20, 1*2)=2, decay=0 → 97
        result.Score.Should().Be(97m);
    }

    [Fact]
    public void Engine_SelfReported_BaseScoreIs20()
    {
        var evidence = new[] { Factor(EvidenceType.SelfReported) };
        var result = ConfidenceEvaluationEngine.Evaluate(evidence, Now, Now);

        // base=20, volumeBonus=2, decay=0 → 22
        result.Score.Should().Be(22m);
    }

    [Fact]
    public void Engine_VolumeBonus_TwoContributors_Adds4Percent()
    {
        var evidence = new[]
        {
            Factor(EvidenceType.SelfReported, "F1"),
            Factor(EvidenceType.SelfReported, "F2")
        };
        var result = ConfidenceEvaluationEngine.Evaluate(evidence, Now, Now);

        // base=20, volumeBonus=min(20, 2*2)=4, decay=0 → 24
        result.Score.Should().Be(24m);
    }

    [Fact]
    public void Engine_VolumeBonus_MaximumCappedAt20Percent()
    {
        // 15 factors → 15*2=30, capped at 20
        var evidence = Enumerable.Range(1, 15)
            .Select(i => Factor(EvidenceType.SelfReported, $"F{i}"))
            .ToList();

        var result = ConfidenceEvaluationEngine.Evaluate(evidence, Now, Now);

        // base=20, volumeBonus=20 (capped), decay=0 → 40
        result.Score.Should().Be(40m);
    }

    [Fact]
    public void Engine_TemporalDecay_Within30Days_NoDecay()
    {
        var refreshTime = Now.AddDays(-15);
        var evidence = new[] { Factor(EvidenceType.ManagerAssessment) };

        var result = ConfidenceEvaluationEngine.Evaluate(evidence, refreshTime, Now);

        // age=15d ≤ 30d → decay=0
        // base=60, volumeBonus=2, decay=0 → 62
        result.Score.Should().Be(62m);
    }

    [Fact]
    public void Engine_TemporalDecay_At60Days_HasDecay()
    {
        var refreshTime = Now.AddDays(-60);
        var evidence = new[] { Factor(EvidenceType.ManagerAssessment) };

        var result = ConfidenceEvaluationEngine.Evaluate(evidence, refreshTime, Now);

        // age=60d, decay=(60-30)*0.2=6
        // base=60, volumeBonus=2, decay=6 → 56
        result.Score.Should().BeApproximately(56m, 1m);
    }

    [Fact]
    public void Engine_TemporalDecay_After90Days_HasLargerDecay()
    {
        var refreshTime = Now.AddDays(-120);
        var evidence = new[] { Factor(EvidenceType.ManagerAssessment) };

        var result = ConfidenceEvaluationEngine.Evaluate(evidence, refreshTime, Now);

        // age=120d, decay=12+(120-90)*0.5=12+15=27
        // base=60, volumeBonus=2, decay=27 → 35
        result.Score.Should().BeApproximately(35m, 1m);
    }

    [Fact]
    public void Engine_FinalScore_AlwaysBetween0And100()
    {
        // Extremely old data + low base score should clamp to 0
        var veryOldRefresh = Now.AddDays(-1000);
        var evidence = new[] { Factor(EvidenceType.SelfReported) };

        var result = ConfidenceEvaluationEngine.Evaluate(evidence, veryOldRefresh, Now);

        result.Score.Should().BeGreaterThanOrEqualTo(0m);
        result.Score.Should().BeLessThanOrEqualTo(100m);
    }

    [Fact]
    public void Engine_NoEvidence_ReturnsZeroScore()
    {
        var result = ConfidenceEvaluationEngine.Evaluate([], Now, Now);
        result.Score.Should().Be(0m);
    }

    [Fact]
    public void Engine_PrimaryEvidenceType_IsHighestWeightFactor()
    {
        var evidence = new[]
        {
            Factor(EvidenceType.SelfReported, "Low"),
            Factor(EvidenceType.SystemCalculated, "High")
        };

        var result = ConfidenceEvaluationEngine.Evaluate(evidence, Now, Now);

        result.EvidenceType.Should().Be("SystemCalculated");
    }
}
