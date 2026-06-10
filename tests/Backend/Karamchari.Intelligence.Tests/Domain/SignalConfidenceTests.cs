// -----------------------------------------------------------------------
// <copyright file="SignalConfidenceTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Intelligence.Domain.Primitives;
using Xunit;

namespace Karamchari.Intelligence.Tests.Domain;

public class SignalConfidenceTests
{
    [Fact]
    public void SignalConfidence_Create_Score0to39_LevelIsLow()
    {
        var result = SignalConfidence.Create(0, "SelfReported");
        result.Level.Should().Be(ConfidenceLevel.Low);
        result.Score.Should().Be(0);
    }

    [Fact]
    public void SignalConfidence_Create_Score25to39_LevelIsLow()
    {
        var result = SignalConfidence.Create(25, "SelfReported");
        result.Level.Should().Be(ConfidenceLevel.Low);
    }

    [Fact]
    public void SignalConfidence_Create_Score40to69_LevelIsMedium()
    {
        var result = SignalConfidence.Create(40, "PeerEndorsement");
        result.Level.Should().Be(ConfidenceLevel.Medium);
    }

    [Fact]
    public void SignalConfidence_Create_Score55to69_LevelIsMedium()
    {
        var result = SignalConfidence.Create(55, "PeerEndorsement");
        result.Level.Should().Be(ConfidenceLevel.Medium);
    }

    [Fact]
    public void SignalConfidence_Create_Score70to89_LevelIsHigh()
    {
        var result = SignalConfidence.Create(70, "ManagerAssessment");
        result.Level.Should().Be(ConfidenceLevel.High);
    }

    [Fact]
    public void SignalConfidence_Create_Score85to89_LevelIsHigh()
    {
        var result = SignalConfidence.Create(85, "HistoricalPerformance");
        result.Level.Should().Be(ConfidenceLevel.High);
    }

    [Fact]
    public void SignalConfidence_Create_Score90to100_LevelIsVerified()
    {
        var result = SignalConfidence.Create(90, "SystemCalculated");
        result.Level.Should().Be(ConfidenceLevel.Verified);
    }

    [Fact]
    public void SignalConfidence_Create_Score100_LevelIsVerified()
    {
        var result = SignalConfidence.Create(100, "VerifiedCertificate");
        result.Level.Should().Be(ConfidenceLevel.Verified);
        result.Score.Should().Be(100);
    }

    [Fact]
    public void SignalConfidence_Create_ScoreAbove100_ThrowsArgumentException()
    {
        var act = () => SignalConfidence.Create(101, "SystemCalculated");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SignalConfidence_Create_ScoreBelow0_ThrowsArgumentException()
    {
        var act = () => SignalConfidence.Create(-1, "SystemCalculated");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SignalConfidence_Create_SetsEvidenceType()
    {
        var result = SignalConfidence.Create(80, "HistoricalPerformance");
        result.EvidenceType.Should().Be("HistoricalPerformance");
    }

    [Fact]
    public void SignalConfidence_Create_SetsEvaluatedAtUtc()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var result = SignalConfidence.Create(50, "ManagerAssessment");
        result.EvaluatedAtUtc.Should().BeAfter(before);
    }
}
