using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Karamchari.Capability.Domain.Primitives;
using Karamchari.Capability.Domain.Succession;
using Xunit;

namespace Karamchari.Capability.Tests.Domain;

public class SuccessionTests
{
    private const string TenantId = "tenant-1";
    private static readonly Guid PositionA = Guid.NewGuid();
    private static readonly Guid RoleReqA = Guid.NewGuid();

    // --- CriticalPosition ---

    [Fact]
    public void CriticalPosition_Designate_MapsAllFields()
    {
        var cp = CriticalPosition.Designate(TenantId, PositionA, RoleReqA, PositionCriticality.MissionCritical);

        cp.TenantId.Should().Be(TenantId);
        cp.PositionId.Should().Be(PositionA);
        cp.RoleRequirementId.Should().Be(RoleReqA);
        cp.Criticality.Should().Be(PositionCriticality.MissionCritical);
        cp.IsActive.Should().BeTrue();
        cp.DeactivatedAtUtc.Should().BeNull();
        cp.Id.Should().NotBeEmpty();
        cp.CreatedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CriticalPosition_Deactivate_SetsInactive()
    {
        var cp = CriticalPosition.Designate(TenantId, PositionA, RoleReqA, PositionCriticality.High);
        cp.Deactivate();

        cp.IsActive.Should().BeFalse();
        cp.DeactivatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void CriticalPosition_UpdateCriticality_ReactivatesAndChangesLevel()
    {
        var cp = CriticalPosition.Designate(TenantId, PositionA, RoleReqA, PositionCriticality.Low);
        cp.Deactivate();
        cp.UpdateCriticality(PositionCriticality.MissionCritical);

        cp.IsActive.Should().BeTrue();
        cp.Criticality.Should().Be(PositionCriticality.MissionCritical);
        cp.DeactivatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void CriticalPosition_Designate_EmptyTenantId_Throws()
    {
        var act = () => CriticalPosition.Designate(string.Empty, PositionA, RoleReqA, PositionCriticality.Low);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CriticalPosition_Designate_EmptyPositionId_Throws()
    {
        var act = () => CriticalPosition.Designate(TenantId, Guid.Empty, RoleReqA, PositionCriticality.Low);
        act.Should().Throw<ArgumentException>();
    }

    // --- SuccessorCandidateProjection ---

    [Fact]
    public void SuccessorCandidateProjection_Create_MapsAllFields()
    {
        var employeeId = Guid.NewGuid();
        var proj = SuccessorCandidateProjection.Create(TenantId, PositionA, employeeId, 85m, CareerReadinessBand.ReadySoon, rank: 2);

        proj.TenantId.Should().Be(TenantId);
        proj.PositionId.Should().Be(PositionA);
        proj.EmployeeId.Should().Be(employeeId);
        proj.ReadinessScore.Should().Be(85m);
        proj.Band.Should().Be(CareerReadinessBand.ReadySoon);
        proj.Rank.Should().Be(2);
    }

    [Fact]
    public void SuccessorCandidateProjection_ApplyRank_UpdatesRank()
    {
        var proj = SuccessorCandidateProjection.Create(TenantId, PositionA, Guid.NewGuid(), 70m, CareerReadinessBand.ReadySoon, rank: 1);
        proj.ApplyRank(3);
        proj.Rank.Should().Be(3);
    }

    // --- BenchStrength ---

    [Fact]
    public void BenchStrength_DeriveRisk_ZeroReadyNow_IsHigh()
    {
        BenchStrength.DeriveRisk(0).Should().Be(SuccessionRiskLevel.High);
    }

    [Fact]
    public void BenchStrength_DeriveRisk_OneReadyNow_IsMedium()
    {
        BenchStrength.DeriveRisk(1).Should().Be(SuccessionRiskLevel.Medium);
    }

    [Fact]
    public void BenchStrength_DeriveRisk_TwoPlusReadyNow_IsLow()
    {
        BenchStrength.DeriveRisk(2).Should().Be(SuccessionRiskLevel.Low);
        BenchStrength.DeriveRisk(5).Should().Be(SuccessionRiskLevel.Low);
    }

    [Fact]
    public void BenchStrength_TotalCandidates_SumsAllBands()
    {
        var bench = new BenchStrength(PositionA, PositionCriticality.High, 2, 5, 11, SuccessionRiskLevel.Low);

        bench.TotalCandidates.Should().Be(18);
        bench.ReadyNowCount.Should().Be(2);
        bench.ReadySoonCount.Should().Be(5);
        bench.NeedsDevelopmentCount.Should().Be(11);
    }
}
