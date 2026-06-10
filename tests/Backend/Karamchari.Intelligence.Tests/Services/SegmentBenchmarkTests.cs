// -----------------------------------------------------------------------
// <copyright file="SegmentBenchmarkTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class SegmentBenchmarkTests
{
    // ── Burnout thresholds ────────────────────────────────────────────────────

    [Fact]
    public void ClassifyBurnout_Technology_LowerThresholds()
    {
        // Technology: moderate=25, high=55, critical=75
        SegmentBenchmark.ClassifyBurnout(24m, WorkforceSegment.Technology).Should().Be(WorkforceRiskLevel.Low);
        SegmentBenchmark.ClassifyBurnout(25m, WorkforceSegment.Technology).Should().Be(WorkforceRiskLevel.Moderate);
        SegmentBenchmark.ClassifyBurnout(55m, WorkforceSegment.Technology).Should().Be(WorkforceRiskLevel.High);
        SegmentBenchmark.ClassifyBurnout(75m, WorkforceSegment.Technology).Should().Be(WorkforceRiskLevel.Critical);
    }

    [Fact]
    public void ClassifyBurnout_EmergencyServices_HigherThresholds()
    {
        // EmergencyServices: moderate=40, high=70, critical=88
        SegmentBenchmark.ClassifyBurnout(39m, WorkforceSegment.EmergencyServices).Should().Be(WorkforceRiskLevel.Low);
        SegmentBenchmark.ClassifyBurnout(40m, WorkforceSegment.EmergencyServices).Should().Be(WorkforceRiskLevel.Moderate);
        SegmentBenchmark.ClassifyBurnout(70m, WorkforceSegment.EmergencyServices).Should().Be(WorkforceRiskLevel.High);
        SegmentBenchmark.ClassifyBurnout(88m, WorkforceSegment.EmergencyServices).Should().Be(WorkforceRiskLevel.Critical);
    }

    [Fact]
    public void ClassifyBurnout_General_StandardThresholds()
    {
        SegmentBenchmark.ClassifyBurnout(29m, WorkforceSegment.General).Should().Be(WorkforceRiskLevel.Low);
        SegmentBenchmark.ClassifyBurnout(30m, WorkforceSegment.General).Should().Be(WorkforceRiskLevel.Moderate);
        SegmentBenchmark.ClassifyBurnout(60m, WorkforceSegment.General).Should().Be(WorkforceRiskLevel.High);
        SegmentBenchmark.ClassifyBurnout(80m, WorkforceSegment.General).Should().Be(WorkforceRiskLevel.Critical);
    }

    [Fact]
    public void ClassifyBurnout_Retail_StandardThresholds()
    {
        // Retail maps to default (30/60/80)
        SegmentBenchmark.ClassifyBurnout(60m, WorkforceSegment.Retail).Should().Be(WorkforceRiskLevel.High);
        SegmentBenchmark.ClassifyBurnout(80m, WorkforceSegment.Retail).Should().Be(WorkforceRiskLevel.Critical);
    }

    // ── Attrition thresholds ──────────────────────────────────────────────────

    [Fact]
    public void ClassifyAttrition_Technology_LowerThresholds()
    {
        // Technology attrition: moderate=25, high=52, critical=72
        SegmentBenchmark.ClassifyAttrition(24m, WorkforceSegment.Technology).Should().Be(WorkforceRiskLevel.Low);
        SegmentBenchmark.ClassifyAttrition(52m, WorkforceSegment.Technology).Should().Be(WorkforceRiskLevel.High);
        SegmentBenchmark.ClassifyAttrition(72m, WorkforceSegment.Technology).Should().Be(WorkforceRiskLevel.Critical);
    }

    [Fact]
    public void ClassifyAttrition_EmergencyServices_HigherThresholds()
    {
        // EmergencyServices attrition: moderate=35, high=65, critical=85
        SegmentBenchmark.ClassifyAttrition(34m, WorkforceSegment.EmergencyServices).Should().Be(WorkforceRiskLevel.Low);
        SegmentBenchmark.ClassifyAttrition(35m, WorkforceSegment.EmergencyServices).Should().Be(WorkforceRiskLevel.Moderate);
        SegmentBenchmark.ClassifyAttrition(65m, WorkforceSegment.EmergencyServices).Should().Be(WorkforceRiskLevel.High);
    }

    [Fact]
    public void SameScore_DifferentSegments_DifferentRiskLevels()
    {
        // Score 60 is High for General but only Moderate for EmergencyServices
        SegmentBenchmark.ClassifyBurnout(60m, WorkforceSegment.General).Should().Be(WorkforceRiskLevel.High);
        SegmentBenchmark.ClassifyBurnout(60m, WorkforceSegment.EmergencyServices).Should().Be(WorkforceRiskLevel.Moderate);
    }
}
