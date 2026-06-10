// -----------------------------------------------------------------------
// <copyright file="CareerReadinessCalculatorTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Capability.Domain.Primitives;
using Karamchari.Capability.Domain.Skills;
using Karamchari.Capability.Services;
using Xunit;

namespace Karamchari.Capability.Tests.Domain;

public class CareerReadinessCalculatorTests
{
    private const string TenantId = "tenant-1";
    private static readonly Guid EmployeeId = Guid.NewGuid();

    private static RoleSkillRequirement NewRequirement(string title = "Test Role")
        => RoleSkillRequirement.Define(TenantId, title, null, null);

    private static CapabilityProfile NewProfile()
        => CapabilityProfile.Initialize(TenantId, EmployeeId);

    // --- Band: ReadyNow ---

    [Fact]
    public void AllSkillsMet_NoCriticalGaps_ReturnsReadyNow()
    {
        var skillA = Guid.NewGuid();
        var skillB = Guid.NewGuid();
        var req = NewRequirement();
        req.AddRequiredSkill(skillA, SkillLevel.Intermediate, isMandatory: true);
        req.AddRequiredSkill(skillB, SkillLevel.Intermediate, isMandatory: true);

        var profile = NewProfile();
        profile.AddVerifiedSkill(skillA, SkillLevel.Advanced, "cert-001", "mgr");
        profile.AddVerifiedSkill(skillB, SkillLevel.Expert, "cert-002", "mgr");

        var (coverage, gapCount, criticalGaps, score, band) = CareerReadinessCalculator.Compute(req, profile);

        band.Should().Be(CareerReadinessBand.ReadyNow);
        coverage.Should().Be(100m);
        gapCount.Should().Be(0);
        criticalGaps.Should().Be(0);
        score.Should().Be(100m);
    }

    [Fact]
    public void CoverageExactly90_NoCriticalGaps_ReturnsReadyNow()
    {
        // 9 of 10 skills met, all shallow gaps (1 level behind)
        var req = NewRequirement();
        var profile = NewProfile();
        var skills = new Guid[10];
        for (int i = 0; i < 10; i++)
        {
            skills[i] = Guid.NewGuid();
            req.AddRequiredSkill(skills[i], SkillLevel.Intermediate, isMandatory: true);
        }
        // Meet 9, shallow gap on last (Novice vs Intermediate = 1 level behind, not critical)
        for (int i = 0; i < 9; i++)
            profile.AddVerifiedSkill(skills[i], SkillLevel.Intermediate, $"cert-{i}", "mgr");
        profile.AddVerifiedSkill(skills[9], SkillLevel.Novice, "cert-9", "mgr"); // 1 level behind

        var (coverage, _, criticalGaps, _, band) = CareerReadinessCalculator.Compute(req, profile);

        coverage.Should().Be(90m);
        criticalGaps.Should().Be(0, "1 level behind is not critical");
        band.Should().Be(CareerReadinessBand.ReadyNow);
    }

    // --- Band: ReadySoon ---

    [Fact]
    public void Coverage70_TwoCriticalGaps_ReturnsReadySoon()
    {
        // 7 of 10 skills met, 2 critical gaps (missing entirely)
        var req = NewRequirement();
        var profile = NewProfile();
        var skills = new Guid[10];
        for (int i = 0; i < 10; i++)
        {
            skills[i] = Guid.NewGuid();
            req.AddRequiredSkill(skills[i], SkillLevel.Intermediate, isMandatory: true);
        }
        for (int i = 0; i < 7; i++)
            profile.AddVerifiedSkill(skills[i], SkillLevel.Intermediate, $"cert-{i}", "mgr");
        // skills[7] and skills[8] missing; skills[9] shallow gap
        profile.AddVerifiedSkill(skills[9], SkillLevel.Novice, "cert-9", "mgr");

        var (coverage, _, criticalGaps, _, band) = CareerReadinessCalculator.Compute(req, profile);

        coverage.Should().Be(70m);
        criticalGaps.Should().Be(2, "two skills missing entirely = two critical gaps");
        band.Should().Be(CareerReadinessBand.ReadySoon);
    }

    [Fact]
    public void Coverage80_OneCriticalGap_ReturnsReadySoon()
    {
        var skillA = Guid.NewGuid();
        var skillB = Guid.NewGuid();
        var skillC = Guid.NewGuid();
        var skillD = Guid.NewGuid();
        var skillE = Guid.NewGuid();
        var req = NewRequirement();
        req.AddRequiredSkill(skillA, SkillLevel.Expert, isMandatory: true);    // critical: Novice vs Expert = 3 levels
        req.AddRequiredSkill(skillB, SkillLevel.Intermediate, isMandatory: true);
        req.AddRequiredSkill(skillC, SkillLevel.Intermediate, isMandatory: true);
        req.AddRequiredSkill(skillD, SkillLevel.Intermediate, isMandatory: true);
        req.AddRequiredSkill(skillE, SkillLevel.Intermediate, isMandatory: true);

        var profile = NewProfile();
        profile.AddVerifiedSkill(skillA, SkillLevel.Novice, "cert-a", "mgr");  // 3 levels behind = critical
        profile.AddVerifiedSkill(skillB, SkillLevel.Intermediate, "cert-b", "mgr");
        profile.AddVerifiedSkill(skillC, SkillLevel.Intermediate, "cert-c", "mgr");
        profile.AddVerifiedSkill(skillD, SkillLevel.Intermediate, "cert-d", "mgr");
        profile.AddVerifiedSkill(skillE, SkillLevel.Intermediate, "cert-e", "mgr");

        var (coverage, gapCount, criticalGaps, _, band) = CareerReadinessCalculator.Compute(req, profile);

        coverage.Should().Be(80m);
        gapCount.Should().Be(1);
        criticalGaps.Should().Be(1, "3 levels behind >= threshold of 2");
        band.Should().Be(CareerReadinessBand.ReadySoon);
    }

    // --- Band: NeedsDevelopment ---

    [Fact]
    public void CoverageBelow70_ReturnsNeedsDevelopment()
    {
        var req = NewRequirement();
        var profile = NewProfile();
        // Only 3 of 10 skills met
        var skills = new Guid[10];
        for (int i = 0; i < 10; i++)
        {
            skills[i] = Guid.NewGuid();
            req.AddRequiredSkill(skills[i], SkillLevel.Intermediate, isMandatory: true);
        }
        for (int i = 0; i < 3; i++)
            profile.AddVerifiedSkill(skills[i], SkillLevel.Intermediate, $"cert-{i}", "mgr");

        var (_, _, _, _, band) = CareerReadinessCalculator.Compute(req, profile);

        band.Should().Be(CareerReadinessBand.NeedsDevelopment);
    }

    [Fact]
    public void Coverage80_ThreeCriticalGaps_ReturnsNeedsDevelopment()
    {
        var req = NewRequirement();
        var profile = NewProfile();
        var skills = new Guid[10];
        for (int i = 0; i < 10; i++)
        {
            skills[i] = Guid.NewGuid();
            req.AddRequiredSkill(skills[i], SkillLevel.Advanced, isMandatory: true);
        }
        // 8 met, 2 missing (critical), first met skill is only Novice for Adv (critical too)
        for (int i = 0; i < 7; i++)
            profile.AddVerifiedSkill(skills[i], SkillLevel.Advanced, $"cert-{i}", "mgr");
        profile.AddVerifiedSkill(skills[7], SkillLevel.Novice, "cert-7", "mgr"); // 2 levels behind = critical
        // skills[8] and skills[9] missing = critical

        var (coverage, _, criticalGaps, _, band) = CareerReadinessCalculator.Compute(req, profile);

        criticalGaps.Should().Be(3);
        band.Should().Be(CareerReadinessBand.NeedsDevelopment, "three critical gaps exceeds ReadySoon threshold of 2");
    }

    // --- ReadinessScore calculation ---

    [Fact]
    public void ReadinessScore_ReducedByCriticalGapPenalty()
    {
        var skillA = Guid.NewGuid();
        var skillB = Guid.NewGuid();
        var req = NewRequirement();
        req.AddRequiredSkill(skillA, SkillLevel.Expert, isMandatory: true);    // employee missing = critical
        req.AddRequiredSkill(skillB, SkillLevel.Intermediate, isMandatory: true); // met

        var profile = NewProfile();
        profile.AddVerifiedSkill(skillB, SkillLevel.Intermediate, "cert-b", "mgr");
        // skillA missing

        var (coverage, _, criticalGaps, readinessScore, _) = CareerReadinessCalculator.Compute(req, profile);

        coverage.Should().Be(50m);
        criticalGaps.Should().Be(1);
        // ReadinessScore = 50 - (1 * 5) = 45
        readinessScore.Should().Be(45m);
    }

    [Fact]
    public void ReadinessScore_ClampedToZero_WhenPenaltyExceedsCoverage()
    {
        var req = NewRequirement();
        var profile = NewProfile();
        // 1 skill met out of 10, rest missing = 9 critical gaps
        var skills = new Guid[10];
        for (int i = 0; i < 10; i++)
        {
            skills[i] = Guid.NewGuid();
            req.AddRequiredSkill(skills[i], SkillLevel.Intermediate, isMandatory: true);
        }
        profile.AddVerifiedSkill(skills[0], SkillLevel.Intermediate, "cert-0", "mgr");

        var (coverage, _, criticalGaps, readinessScore, _) = CareerReadinessCalculator.Compute(req, profile);

        coverage.Should().Be(10m);
        criticalGaps.Should().Be(9);
        // 10 - (9 * 5) = 10 - 45 = -35, clamped to 0
        readinessScore.Should().Be(0m);
    }

    // --- Edge cases ---

    [Fact]
    public void NoRequiredSkills_ReturnsReadyNow100()
    {
        var req = NewRequirement();
        var profile = NewProfile();

        var (coverage, gapCount, criticalGaps, readinessScore, band) = CareerReadinessCalculator.Compute(req, profile);

        coverage.Should().Be(100m);
        gapCount.Should().Be(0);
        criticalGaps.Should().Be(0);
        readinessScore.Should().Be(100m);
        band.Should().Be(CareerReadinessBand.ReadyNow);
    }

    [Fact]
    public void ShallowGap_OneLevelBehind_NotCritical()
    {
        var skillId = Guid.NewGuid();
        var req = NewRequirement();
        req.AddRequiredSkill(skillId, SkillLevel.Intermediate, isMandatory: true);

        var profile = NewProfile();
        profile.AddVerifiedSkill(skillId, SkillLevel.Novice, "cert-001", "mgr"); // 1 level behind

        var (_, _, criticalGaps, _, _) = CareerReadinessCalculator.Compute(req, profile);

        criticalGaps.Should().Be(0, "1 level behind is below the critical threshold of 2");
    }

    [Fact]
    public void ExactlyAtCriticalThreshold_TwoLevelsBehind_IsCritical()
    {
        var skillId = Guid.NewGuid();
        var req = NewRequirement();
        req.AddRequiredSkill(skillId, SkillLevel.Advanced, isMandatory: true);

        var profile = NewProfile();
        profile.AddVerifiedSkill(skillId, SkillLevel.Novice, "cert-001", "mgr"); // 2 levels behind

        var (_, _, criticalGaps, _, _) = CareerReadinessCalculator.Compute(req, profile);

        criticalGaps.Should().Be(1, "exactly 2 levels behind meets the critical threshold");
    }
}
