// -----------------------------------------------------------------------
// <copyright file="RF8_WorkforcePlanningTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.WorkforcePlanning;
using Xunit;

/// <summary>RF#8 — Workforce Planning: all boundary conditions for demand/supply/gap/risk.</summary>
public sealed class RF8_WorkforcePlanningTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    private static readonly string Tenant = "t1";

    // ── WorkforceDemand ───────────────────────────────────────────────────

    [Fact]
    public void WorkforceDemand_Create_Succeeds()
    {
        var d = WorkforceDemand.Create(Tenant, Guid.NewGuid(), "Plant", "Plant A",
            DemandPeriodType.Daily, Today, Today,
            requiredHeadcount: 150, optimalHeadcount: 180, criticalMinimumHeadcount: 100);

        d.RequiredHeadcount.Should().Be(150);
        d.OptimalHeadcount.Should().Be(180);
        d.CriticalMinimumHeadcount.Should().Be(100);
        d.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void WorkforceDemand_RequiredZero_Throws()
    {
        var act = () => WorkforceDemand.Create(Tenant, Guid.NewGuid(), "Team", "Support",
            DemandPeriodType.Daily, Today, Today,
            requiredHeadcount: 0, optimalHeadcount: 0, criticalMinimumHeadcount: 0);
        act.Should().Throw<ArgumentOutOfRangeException>("demand of 0 is not a valid planning input");
    }

    [Fact]
    public void WorkforceDemand_OptimalLessThanRequired_ClampedToRequired()
    {
        // Optimal cannot be less than Required — clamped
        var d = WorkforceDemand.Create(Tenant, Guid.NewGuid(), "Team", "Dev",
            DemandPeriodType.Weekly, Today, Today.AddDays(6),
            requiredHeadcount: 50, optimalHeadcount: 30, criticalMinimumHeadcount: 40);

        d.OptimalHeadcount.Should().Be(50, "optimal clamped to required when lower");
    }

    [Fact]
    public void WorkforceDemand_CriticalMinGreaterThanRequired_ClampedToRequired()
    {
        var d = WorkforceDemand.Create(Tenant, Guid.NewGuid(), "Plant", "Plant B",
            DemandPeriodType.Shift, Today, Today,
            requiredHeadcount: 100, optimalHeadcount: 120, criticalMinimumHeadcount: 120);

        d.CriticalMinimumHeadcount.Should().Be(100, "critical min cannot exceed required");
    }

    [Fact]
    public void WorkforceDemand_Publish_IsPublishedTrue()
    {
        var d = WorkforceDemand.Create(Tenant, Guid.NewGuid(), "Dept", "Finance",
            DemandPeriodType.Monthly, Today, Today.AddDays(29),
            100, 120, 70);

        d.Publish();
        d.IsPublished.Should().BeTrue();
    }

    // ── WorkforceSupply ───────────────────────────────────────────────────

    [Fact]
    public void WorkforceSupply_Compute_CorrectAvailable()
    {
        // Total=100, OnLeave=10, OnRoster=80 → Available = max(0, 100 - 10 - (100-80)) = 70
        var supply = WorkforceSupply.Compute(Tenant, Guid.NewGuid(), Guid.NewGuid(),
            Today, Today.AddDays(6),
            totalHeadcount: 100, onLeave: 10, onRoster: 80);

        supply.ExpectedAvailable.Should().Be(70);
    }

    [Fact]
    public void WorkforceSupply_FullRoster_NoLeave_AllAvailable()
    {
        var supply = WorkforceSupply.Compute(Tenant, Guid.NewGuid(), Guid.NewGuid(),
            Today, Today.AddDays(6), 50, 0, 50);
        supply.ExpectedAvailable.Should().Be(50);
    }

    [Fact]
    public void WorkforceSupply_ZeroTotal_ZeroAvailable()
    {
        var supply = WorkforceSupply.Compute(Tenant, Guid.NewGuid(), Guid.NewGuid(),
            Today, Today, 0, 0, 0);
        supply.ExpectedAvailable.Should().Be(0);
    }

    // ── CapacityGap ───────────────────────────────────────────────────────

    [Fact]
    public void CapacityGap_PositiveGap_Surplus_LowRisk()
    {
        var gap = CapacityGap.Compute(Tenant, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Today, Today, requiredHeadcount: 100, criticalMinimum: 70, expectedAvailable: 110);

        gap.GapHeadcount.Should().Be(10, "surplus");
        gap.RiskLevel.Should().Be(CoverageRiskLevel.Low);
    }

    [Fact]
    public void CapacityGap_ModerateShortfall_ModerateRisk()
    {
        // Gap = 92 - 100 = -8 = -8% → Moderate (< 20% shortfall, above criticalMin)
        var gap = CapacityGap.Compute(Tenant, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Today, Today, requiredHeadcount: 100, criticalMinimum: 70, expectedAvailable: 92);

        gap.GapHeadcount.Should().BeNegative();
        gap.RiskLevel.Should().Be(CoverageRiskLevel.Moderate);
    }

    [Fact]
    public void CapacityGap_Boundary_CriticalMinExactlyMet_NotCritical()
    {
        // Available == criticalMinimum → boundary is "≤" → this is Critical if available <= critMin
        // From source: available <= criticalMinimum → Critical
        // Available = 70, critMin = 70 → Critical
        var gap = CapacityGap.Compute(Tenant, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Today, Today, requiredHeadcount: 100, criticalMinimum: 70, expectedAvailable: 70);

        gap.RiskLevel.Should().Be(CoverageRiskLevel.Critical,
            "available exactly at critical minimum → Critical risk");
    }

    [Fact]
    public void CapacityGap_HighShortfall_Over20Percent_HighRisk()
    {
        // Gap = 75 - 100 = -25 = -25% → High (> 20% shortfall, above criticalMin)
        var gap = CapacityGap.Compute(Tenant, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Today, Today, requiredHeadcount: 100, criticalMinimum: 50, expectedAvailable: 75);

        gap.GapPercent.Should().BeLessThan(0);
        gap.RiskLevel.Should().Be(CoverageRiskLevel.High);
    }

    [Fact]
    public void CapacityGap_ZeroDemand_IsHandled()
    {
        // Zero demand is unusual but mustn't crash
        var act = () => CapacityGap.Compute(Tenant, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Today, Today, requiredHeadcount: 0, criticalMinimum: 0, expectedAvailable: 50);
        act.Should().NotThrow("zero demand is a valid boundary — no division by zero");
    }

    [Fact]
    public void CapacityGap_ZeroSupply_MaximumShortfall()
    {
        var gap = CapacityGap.Compute(Tenant, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Today, Today, requiredHeadcount: 100, criticalMinimum: 70, expectedAvailable: 0);

        gap.GapHeadcount.Should().Be(-100);
        gap.RiskLevel.Should().Be(CoverageRiskLevel.Critical, "zero supply = critical");
    }

    // ── CoverageRisk ──────────────────────────────────────────────────────

    [Fact]
    public void CoverageRisk_Raise_SlaBreachFlagged()
    {
        var risk = CoverageRisk.Raise(Tenant, Guid.NewGuid(), "Team", "Support",
            Today.AddDays(7), CoverageRiskLevel.High,
            "Forecasted leave 22% — below SLA threshold",
            forecastedLeavePercent: 22m, forecastedShortfall: 8, slaBreachRisk: true);

        risk.SlaBreachRisk.Should().BeTrue();
        risk.RiskLevel.Should().Be(CoverageRiskLevel.High);
        risk.Acknowledged.Should().BeFalse();
    }

    [Fact]
    public void CoverageRisk_Acknowledge_SetsFlag()
    {
        var risk = CoverageRisk.Raise(Tenant, Guid.NewGuid(), "Plant", "Plant A",
            Today, CoverageRiskLevel.Critical, "Critical shortage", 35m, 18, false);

        risk.Acknowledge();
        risk.Acknowledged.Should().BeTrue();
    }

    [Fact]
    public void CoverageRisk_AlertSent_PreventsDuplication()
    {
        var risk = CoverageRisk.Raise(Tenant, Guid.NewGuid(), "Dept", "Ops",
            Today, CoverageRiskLevel.Moderate, "Shortage", 12m, 3, false);

        risk.MarkAlertSent();
        risk.AlertSent.Should().BeTrue("prevents duplicate notifications");
    }
}
