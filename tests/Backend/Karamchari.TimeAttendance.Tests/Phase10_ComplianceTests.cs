// -----------------------------------------------------------------------
// <copyright file="Phase10_ComplianceTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Persistence;
using Xunit;

/// <summary>Phase 10 — Compliance: Bradford Factor formula, risk levels, leave liability.</summary>
public sealed class Phase10_ComplianceTests
{
    // ── Bradford Factor B = S² × D ────────────────────────────────────────

    private static BradfordFactorScore MakeScore(int spells, decimal days) =>
        new() { TenantId = "t1", EmployeeId = Guid.NewGuid(), AbsenceEpisodes = spells, TotalAbsenceDays = days };

    [Fact]
    public void Bradford_Formula_10Spells_10Days_Equals1000()
    {
        MakeScore(10, 10m).Score.Should().Be(1000m);
    }

    [Fact]
    public void Bradford_Formula_1Spell_10Days_Equals10()
    {
        MakeScore(1, 10m).Score.Should().Be(10m);
    }

    [Fact]
    public void Bradford_Formula_5Spells_5Days_Equals125()
    {
        MakeScore(5, 5m).Score.Should().Be(125m);
    }

    [Fact]
    public void Bradford_Formula_ZeroSpells_ScoreIsZero()
    {
        MakeScore(0, 0m).Score.Should().Be(0m);
    }

    // ── Risk Level Thresholds ─────────────────────────────────────────────

    [Theory]
    [InlineData(0, BradfordRiskLevel.Low)]
    [InlineData(44, BradfordRiskLevel.Low)]
    [InlineData(45, BradfordRiskLevel.Medium)]
    [InlineData(99, BradfordRiskLevel.Medium)]
    [InlineData(100, BradfordRiskLevel.High)]
    [InlineData(399, BradfordRiskLevel.High)]
    [InlineData(400, BradfordRiskLevel.Critical)]
    [InlineData(1000, BradfordRiskLevel.Critical)]
    public void Bradford_RiskLevel_MatchesThresholds(int score, BradfordRiskLevel expected)
    {
        BradfordFactorScore.CalculateRiskLevel((decimal)score).Should().Be(expected);
    }

    // ── Leave Liability Snapshot ──────────────────────────────────────────

    [Fact]
    public void LeaveLiability_PositiveCase_ComputesCorrectly()
    {
        var snap = LeaveLiabilitySnapshot.Create(
            "t1", 2026, 6, null, null,
            earnedBalanceDays: 1000m, carryForwardDays: 200m,
            encashableBalanceDays: 300m, lopDaysYtd: 50m,
            estimatedLiabilityAmount: 500000m,
            currencyCode: "INR", employeeCount: 100, computedBy: "scheduler");

        snap.TotalEarnedLeaveBalanceDays.Should().Be(1000m);
        snap.EstimatedLiabilityAmount.Should().Be(500000m);
        snap.CurrencyCode.Should().Be("INR");
    }

    [Fact]
    public void LeaveLiability_MonthOutOfRange_Throws()
    {
        var act = () => LeaveLiabilitySnapshot.Create(
            "t1", 2026, 13, null, null, 0, 0, 0, 0, 0, "USD", 0, "sys");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LeaveLiability_Month0_Throws()
    {
        var act = () => LeaveLiabilitySnapshot.Create(
            "t1", 2026, 0, null, null, 0, 0, 0, 0, 0, "USD", 0, "sys");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── Compliance Violations ─────────────────────────────────────────────

    [Fact]
    public void ComplianceViolation_Raise_SetsOpenStatus()
    {
        var v = ComplianceViolation.Raise(
            "t1", Guid.NewGuid(),
            ComplianceViolationType.ConsecutiveWorkingDays,
            ComplianceSeverity.Critical,
            "Worked 16 consecutive days",
            """{"consecutiveDays":16,"threshold":14}""");

        v.Status.Should().Be(ComplianceViolationStatus.Open);
        v.Severity.Should().Be(ComplianceSeverity.Critical);
    }

    [Fact]
    public void ComplianceViolation_Acknowledge_SetsAcknowledgedStatus()
    {
        var v = ComplianceViolation.Raise("t1", Guid.NewGuid(),
            ComplianceViolationType.CompOffExpiryRisk, ComplianceSeverity.Warning,
            "CompOff expiring in 7 days", "{}");

        v.Acknowledge("hr-admin");

        v.Status.Should().Be(ComplianceViolationStatus.Acknowledged);
        v.AcknowledgedBy.Should().Be("hr-admin");
    }

    [Fact]
    public void ComplianceViolation_Resolve_SetsResolvedStatus()
    {
        var v = ComplianceViolation.Raise("t1", Guid.NewGuid(),
            ComplianceViolationType.MaternityEntitlementNotGranted, ComplianceSeverity.Critical,
            "Maternity not granted", "{}");

        v.Resolve();

        v.Status.Should().Be(ComplianceViolationStatus.Resolved);
        v.ResolvedAt.Should().NotBeNull();
    }

    // ── Legal Hold ────────────────────────────────────────────────────────

    [Fact]
    public void LegalHold_Create_ActiveByDefault()
    {
        var hold = Domain.Leaves.LegalHold.Create("t1",
            Domain.Leaves.LegalHoldScope.Employee, Guid.NewGuid(),
            "Labour dispute", "CASE-2026-001", "hr-director");

        hold.Status.Should().Be(Domain.Leaves.LegalHoldStatus.Active);
    }

    [Fact]
    public void LegalHold_Lift_SetsLiftedStatus()
    {
        var hold = Domain.Leaves.LegalHold.Create("t1",
            Domain.Leaves.LegalHoldScope.Employee, Guid.NewGuid(),
            "Labour dispute", "CASE-2026-001", "hr-director");

        hold.Lift("hr-director", "Case settled");

        hold.Status.Should().Be(Domain.Leaves.LegalHoldStatus.Lifted);
        hold.LiftedBy.Should().Be("hr-director");
    }

    [Fact]
    public void LegalHold_LiftTwice_Throws()
    {
        var hold = Domain.Leaves.LegalHold.Create("t1",
            Domain.Leaves.LegalHoldScope.Employee, Guid.NewGuid(),
            "Labour dispute", "CASE-2026-002", "hr-director");
        hold.Lift("hr-director", "Settled");

        var act = () => hold.Lift("hr-director", "Again");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LegalHold_IsActiveOn_BeforeInitiation_ReturnsFalse()
    {
        var hold = Domain.Leaves.LegalHold.Create("t1",
            Domain.Leaves.LegalHoldScope.Tenant, null,
            "Investigation", "CASE-2026-003", "legal");

        var before = hold.InitiatedAt.AddHours(-1);
        hold.IsActiveOn(before).Should().BeFalse();
    }
}
