// -----------------------------------------------------------------------
// <copyright file="CareerPathTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Capability.Domain.Pathing;
using Karamchari.Capability.Domain.Primitives;
using Xunit;

namespace Karamchari.Capability.Tests.Domain;

public class CareerPathTests
{
    private const string TenantId = "tenant-1";
    private static readonly Guid RoleA = Guid.NewGuid();
    private static readonly Guid RoleB = Guid.NewGuid();
    private static readonly Guid RoleC = Guid.NewGuid();

    // --- CareerProgressionEdge ---

    [Fact]
    public void Edge_Add_MapsAllFields()
    {
        var edge = CareerProgressionEdge.Add(TenantId, RoleA, RoleB, weight: 1.5m);

        edge.TenantId.Should().Be(TenantId);
        edge.FromRoleRequirementId.Should().Be(RoleA);
        edge.ToRoleRequirementId.Should().Be(RoleB);
        edge.Weight.Should().Be(1.5m);
        edge.CreatedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Edge_Add_DefaultWeight_IsOne()
    {
        var edge = CareerProgressionEdge.Add(TenantId, RoleA, RoleB);
        edge.Weight.Should().Be(1.0m);
    }

    [Fact]
    public void Edge_Add_SelfReferential_Throws()
    {
        var act = () => CareerProgressionEdge.Add(TenantId, RoleA, RoleA);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Edge_Add_ZeroWeight_Throws()
    {
        var act = () => CareerProgressionEdge.Add(TenantId, RoleA, RoleB, weight: 0m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Edge_Add_NegativeWeight_Throws()
    {
        var act = () => CareerProgressionEdge.Add(TenantId, RoleA, RoleB, weight: -1m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // --- CareerPathResult / CareerPathStep ---

    [Fact]
    public void CareerPathStep_Priority_Missing_IsOne()
    {
        var step = new CareerPathStep(
            SkillId: Guid.NewGuid(),
            SkillName: "Incident Management",
            RequiredLevel: SkillLevel.Intermediate,
            CurrentLevel: null,
            LevelsBehind: (int)SkillLevel.Intermediate,
            IsCritical: true,
            Priority: 1);

        step.Priority.Should().Be(1);
        step.IsCritical.Should().BeTrue();
        step.CurrentLevel.Should().BeNull();
    }

    [Fact]
    public void CareerPathResult_ReadyNow_HasEmptySteps()
    {
        var result = new CareerPathResult
        {
            EmployeeId = Guid.NewGuid(),
            TargetRoleRequirementId = RoleB,
            TargetRoleTitle = "Site Supervisor",
            CurrentBand = CareerReadinessBand.ReadyNow,
            CurrentReadinessScore = 95m,
            CurrentCoveragePercent = 100m,
            MissingSkillCount = 0,
            CriticalGapCount = 0,
            Steps = [],
            Path = [],
        };

        result.Steps.Should().BeEmpty("ReadyNow employee needs no further development");
        result.CurrentBand.Should().Be(CareerReadinessBand.ReadyNow);
    }

    [Fact]
    public void CareerPathResult_StepsOrderedByPriority()
    {
        var steps = new List<CareerPathStep>
        {
            new(Guid.NewGuid(), "Shallow Skill", SkillLevel.Advanced, SkillLevel.Intermediate, 1, false, 3),
            new(Guid.NewGuid(), "Critical Skill", SkillLevel.Expert, SkillLevel.Novice, 3, true, 2),
            new(Guid.NewGuid(), "Missing Skill", SkillLevel.Intermediate, null, 2, true, 1),
        };

        var sorted = steps.OrderBy(s => s.Priority).ThenByDescending(s => s.LevelsBehind).ToList();

        sorted[0].Priority.Should().Be(1);
        sorted[1].Priority.Should().Be(2);
        sorted[2].Priority.Should().Be(3);
    }

    [Fact]
    public void CareerProgressionHop_StepIndex_IsZeroBased()
    {
        var hops = new List<CareerProgressionHop>
        {
            new(RoleA, "Security Guard", 0),
            new(RoleB, "Senior Guard", 1),
            new(RoleC, "Site Supervisor", 2),
        };

        hops[0].StepIndex.Should().Be(0);
        hops[2].StepIndex.Should().Be(2);
        hops[2].RoleTitle.Should().Be("Site Supervisor");
    }
}
