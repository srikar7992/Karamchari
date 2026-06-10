// -----------------------------------------------------------------------
// <copyright file="InternalMobilityProjectionTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Capability.Domain.Mobility;
using Karamchari.Capability.Domain.Primitives;
using Xunit;

namespace Karamchari.Capability.Tests.Domain;

public class InternalMobilityProjectionTests
{
    private const string TenantId = "tenant-1";
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid VacancyId = Guid.NewGuid();
    private static readonly Guid PositionId = Guid.NewGuid();
    private static readonly Guid RoleRequirementId = Guid.NewGuid();

    private static InternalMobilityProjection Build(
        decimal coveragePercent = 100m,
        decimal readinessScore = 100m,
        CareerReadinessBand band = CareerReadinessBand.ReadyNow,
        int missingSkillCount = 0,
        int criticalGapCount = 0)
        => InternalMobilityProjection.Create(
            TenantId, EmployeeId, VacancyId, PositionId, RoleRequirementId,
            coveragePercent, readinessScore, band, missingSkillCount, criticalGapCount);

    // --- Eligible flag ---

    [Fact]
    public void ReadyNow_IsEligible()
    {
        var projection = Build(band: CareerReadinessBand.ReadyNow);
        projection.Eligible.Should().BeTrue();
    }

    [Fact]
    public void ReadySoon_IsEligible()
    {
        var projection = Build(band: CareerReadinessBand.ReadySoon, coveragePercent: 75m, readinessScore: 65m);
        projection.Eligible.Should().BeTrue();
    }

    [Fact]
    public void NeedsDevelopment_NotEligible()
    {
        var projection = Build(band: CareerReadinessBand.NeedsDevelopment, coveragePercent: 40m, readinessScore: 15m);
        projection.Eligible.Should().BeFalse();
    }

    // --- Field mapping ---

    [Fact]
    public void Create_MapsAllFields()
    {
        var projection = Build(
            coveragePercent: 82m,
            readinessScore: 72m,
            band: CareerReadinessBand.ReadySoon,
            missingSkillCount: 2,
            criticalGapCount: 1);

        projection.TenantId.Should().Be(TenantId);
        projection.EmployeeId.Should().Be(EmployeeId);
        projection.VacancyId.Should().Be(VacancyId);
        projection.PositionId.Should().Be(PositionId);
        projection.RoleRequirementId.Should().Be(RoleRequirementId);
        projection.CoveragePercent.Should().Be(82m);
        projection.ReadinessScore.Should().Be(72m);
        projection.Band.Should().Be(CareerReadinessBand.ReadySoon);
        projection.MissingSkillCount.Should().Be(2);
        projection.CriticalGapCount.Should().Be(1);
        projection.Eligible.Should().BeTrue();
        projection.LastUpdatedUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    // --- Update ---

    [Fact]
    public void Update_OverwritesAllFields_AndRecalculatesEligible()
    {
        var projection = Build(band: CareerReadinessBand.ReadyNow);
        projection.Eligible.Should().BeTrue();

        // Skill degraded — employee now NeedsDevelopment
        projection.Update(
            coveragePercent: 50m,
            readinessScore: 20m,
            band: CareerReadinessBand.NeedsDevelopment,
            missingSkillCount: 4,
            criticalGapCount: 3);

        projection.CoveragePercent.Should().Be(50m);
        projection.ReadinessScore.Should().Be(20m);
        projection.Band.Should().Be(CareerReadinessBand.NeedsDevelopment);
        projection.MissingSkillCount.Should().Be(4);
        projection.CriticalGapCount.Should().Be(3);
        projection.Eligible.Should().BeFalse("band changed to NeedsDevelopment");
    }

    [Fact]
    public void Update_FromNeedsDevelopment_ToReadyNow_SetsEligible()
    {
        var projection = Build(band: CareerReadinessBand.NeedsDevelopment, coveragePercent: 50m, readinessScore: 20m);
        projection.Eligible.Should().BeFalse();

        projection.Update(
            coveragePercent: 95m,
            readinessScore: 95m,
            band: CareerReadinessBand.ReadyNow,
            missingSkillCount: 0,
            criticalGapCount: 0);

        projection.Eligible.Should().BeTrue("promoted to ReadyNow after skill development");
    }

    // --- MobilityVacancy ---

    [Fact]
    public void MobilityVacancy_Open_MapsAllFields()
    {
        var opened = DateTimeOffset.UtcNow;
        var vacancy = MobilityVacancy.Open(TenantId, VacancyId, PositionId, RoleRequirementId, opened);

        vacancy.TenantId.Should().Be(TenantId);
        vacancy.VacancyId.Should().Be(VacancyId);
        vacancy.PositionId.Should().Be(PositionId);
        vacancy.RoleRequirementId.Should().Be(RoleRequirementId);
        vacancy.OpenedUtc.Should().Be(opened);
    }

    // --- Ranking invariant ---

    [Fact]
    public void ReadyNow_HasHigherScoreThan_ReadySoon_WhenOrderedDescending()
    {
        var readyNow = Build(band: CareerReadinessBand.ReadyNow, readinessScore: 95m);
        var readySoon = Build(band: CareerReadinessBand.ReadySoon, readinessScore: 72m);

        readyNow.ReadinessScore.Should().BeGreaterThan(readySoon.ReadinessScore,
            "ReadyNow candidates rank higher in the mobility index");
    }

    [Fact]
    public void TwoReadyNow_DifferentScores_RankByScore()
    {
        var senior = Build(band: CareerReadinessBand.ReadyNow, readinessScore: 98m);
        var junior = Build(band: CareerReadinessBand.ReadyNow, readinessScore: 91m);

        var ranked = new[] { junior, senior }
            .OrderByDescending(p => p.ReadinessScore)
            .ToList();

        ranked[0].ReadinessScore.Should().Be(98m);
        ranked[1].ReadinessScore.Should().Be(91m);
    }
}
