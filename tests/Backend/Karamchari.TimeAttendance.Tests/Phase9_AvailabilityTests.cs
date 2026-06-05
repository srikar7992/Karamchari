namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Persistence;
using Xunit;

/// <summary>Phase 9 — Workforce Availability: formula correctness, boundary, division-by-zero.</summary>
public sealed class Phase9_AvailabilityTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Availability_NormalScenario_80Percent()
    {
        // Scheduled=100, OnLeave=10, Absent=10 → Present=80 → 80%
        var idx = WorkforceAvailabilityIndex.Compute(
            "t1", Today, WorkforceGroupType.Team, Guid.NewGuid(), "Engineering",
            scheduled: 100, onLeave: 10, absent: 10);

        idx.Present.Should().Be(80);
        idx.AvailabilityPercent.Should().Be(80.0m);
    }

    [Fact]
    public void Availability_AllPresent_100Percent()
    {
        var idx = WorkforceAvailabilityIndex.Compute(
            "t1", Today, WorkforceGroupType.Department, Guid.NewGuid(), "Support",
            scheduled: 50, onLeave: 0, absent: 0);

        idx.AvailabilityPercent.Should().Be(100.0m);
        idx.BelowThreshold.Should().BeFalse();
    }

    [Fact]
    public void Availability_Scheduled0_DivisionByZero_DoesNotThrow()
    {
        // Scheduled=0 must not throw. AvailabilityPercent computed property returns 0%
        // when Scheduled=0 (no staff scheduled → undefined, represented as 0%).
        var act = () => WorkforceAvailabilityIndex.Compute(
            "t1", Today, WorkforceGroupType.Plant, Guid.NewGuid(), "PlantX",
            scheduled: 0, onLeave: 0, absent: 0);

        var idx = act.Should().NotThrow().Subject;
        idx.AvailabilityPercent.Should().Be(0.0m, "Scheduled=0: no division by zero, returns 0");
    }

    [Fact]
    public void Availability_OnLeave_Plus_Absent_ExceedsScheduled_PresentClampedToZero()
    {
        var idx = WorkforceAvailabilityIndex.Compute(
            "t1", Today, WorkforceGroupType.Location, Guid.NewGuid(), "Site A",
            scheduled: 10, onLeave: 8, absent: 5); // 8+5=13 > 10

        idx.Present.Should().Be(0, "present cannot go negative");
        idx.AvailabilityPercent.Should().Be(0.0m);
    }

    [Fact]
    public void Availability_BelowThreshold_FlagsCorrectly()
    {
        var idx = WorkforceAvailabilityIndex.Compute(
            "t1", Today, WorkforceGroupType.Team, Guid.NewGuid(), "Team A",
            scheduled: 100, onLeave: 40, absent: 0, thresholdPercent: 70m);

        idx.AvailabilityPercent.Should().Be(60.0m);
        idx.BelowThreshold.Should().BeTrue();
    }

    [Fact]
    public void Availability_ExactlyAtThreshold_NotBelowThreshold()
    {
        var idx = WorkforceAvailabilityIndex.Compute(
            "t1", Today, WorkforceGroupType.Team, Guid.NewGuid(), "Team B",
            scheduled: 100, onLeave: 30, absent: 0, thresholdPercent: 70m);

        idx.AvailabilityPercent.Should().Be(70.0m);
        idx.BelowThreshold.Should().BeFalse();
    }

    [Fact]
    public void Availability_Refresh_UpdatesAllFields()
    {
        var idx = WorkforceAvailabilityIndex.Compute(
            "t1", Today, WorkforceGroupType.Team, Guid.NewGuid(), "Team C",
            scheduled: 100, onLeave: 10, absent: 0);

        idx.Refresh(scheduled: 100, onLeave: 5, absent: 0);

        idx.Present.Should().Be(95);
        idx.AvailabilityPercent.Should().Be(95.0m);
    }

    // ── Trend ─────────────────────────────────────────────────────────────

    [Fact]
    public void AvailabilityTrend_EmptyDays_ReturnsZeroAverages()
    {
        var trend = WorkforceAvailabilityTrend.Compute(
            "t1", Guid.NewGuid(), "Team", "Engineering",
            TrendWindow.Days30, Today, []);

        trend.AverageAvailabilityPercent.Should().Be(0m);
        trend.IsAtRisk.Should().BeFalse();
    }

    [Fact]
    public void AvailabilityTrend_AllBelowThreshold_IsAtRisk()
    {
        var dailyValues = Enumerable.Repeat(60m, 30).ToList();
        var trend = WorkforceAvailabilityTrend.Compute(
            "t1", Guid.NewGuid(), "Plant", "Plant A",
            TrendWindow.Days30, Today, dailyValues, thresholdPercent: 70m);

        trend.AverageAvailabilityPercent.Should().Be(60.0m);
        trend.IsAtRisk.Should().BeTrue();
    }

    [Fact]
    public void AvailabilityTrend_ForecastDecline_FlagsBelowThreshold()
    {
        // Simulate: first 23 days at 90%, last 7 days dropping to 65%
        var days = Enumerable.Repeat(90m, 23).Concat(Enumerable.Repeat(65m, 7)).ToList();
        var trend = WorkforceAvailabilityTrend.Compute(
            "t1", Guid.NewGuid(), "Dept", "Support",
            TrendWindow.Days30, Today, days, thresholdPercent: 70m);

        // Forecast: slope = 65 - 90 = -25 → projected next week ≈ 65 - 25 = 40
        trend.ForecastBelowThreshold.Should().BeTrue("declining trend forecasts below threshold");
    }

    // ── Capacity Gap ──────────────────────────────────────────────────────

    [Fact]
    public void CapacityGap_Surplus_IsLowRisk()
    {
        var gap = Domain.WorkforcePlanning.CapacityGap.Compute(
            "t1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Today, Today.AddDays(6),
            requiredHeadcount: 100, criticalMinimum: 70, expectedAvailable: 110);

        gap.GapHeadcount.Should().Be(10);
        gap.RiskLevel.Should().Be(Domain.WorkforcePlanning.CoverageRiskLevel.Low);
    }

    [Fact]
    public void CapacityGap_BelowCriticalMin_IsCritical()
    {
        var gap = Domain.WorkforcePlanning.CapacityGap.Compute(
            "t1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Today, Today.AddDays(6),
            requiredHeadcount: 100, criticalMinimum: 70, expectedAvailable: 65);

        gap.RiskLevel.Should().Be(Domain.WorkforcePlanning.CoverageRiskLevel.Critical);
    }

    [Fact]
    public void CapacityGap_ModerateShortfall_IsModerate()
    {
        var gap = Domain.WorkforcePlanning.CapacityGap.Compute(
            "t1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Today, Today.AddDays(6),
            requiredHeadcount: 100, criticalMinimum: 70, expectedAvailable: 92); // -8%

        gap.RiskLevel.Should().Be(Domain.WorkforcePlanning.CoverageRiskLevel.Moderate);
    }
}
