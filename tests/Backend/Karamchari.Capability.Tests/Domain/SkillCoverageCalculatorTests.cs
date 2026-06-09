using FluentAssertions;
using Karamchari.Capability.Domain.Primitives;
using Karamchari.Capability.Domain.Skills;
using Karamchari.Capability.Services;
using Xunit;

namespace Karamchari.Capability.Tests.Domain;

public class SkillCoverageCalculatorTests
{
    private const string TenantId = "tenant-1";
    private static readonly Guid EmployeeId = Guid.NewGuid();

    private static RoleSkillRequirement NewRequirement(string title = "Test Role")
    {
        return RoleSkillRequirement.Define(TenantId, title, null, null);
    }

    private static CapabilityProfile NewProfile()
        => CapabilityProfile.Initialize(TenantId, EmployeeId);

    // --- Level comparison ---

    [Fact]
    public void BelowMinimumLevel_NotMatched()
    {
        var skillId = Guid.NewGuid();
        var req = NewRequirement();
        req.AddRequiredSkill(skillId, SkillLevel.Intermediate, isMandatory: true);

        var profile = NewProfile();
        profile.AddVerifiedSkill(skillId, SkillLevel.Novice, "cert-001", "mgr");

        var (matched, missing, total) = SkillCoverageCalculator.Compute(req, profile);

        matched.Should().Be(0);
        missing.Should().Be(1);
        total.Should().Be(1);
    }

    [Fact]
    public void AtMinimumLevel_Matched()
    {
        var skillId = Guid.NewGuid();
        var req = NewRequirement();
        req.AddRequiredSkill(skillId, SkillLevel.Intermediate, isMandatory: true);

        var profile = NewProfile();
        profile.AddVerifiedSkill(skillId, SkillLevel.Intermediate, "cert-001", "mgr");

        var (matched, missing, total) = SkillCoverageCalculator.Compute(req, profile);

        matched.Should().Be(1);
        missing.Should().Be(0);
        total.Should().Be(1);
    }

    [Fact]
    public void AboveMinimumLevel_Matched()
    {
        var skillId = Guid.NewGuid();
        var req = NewRequirement();
        req.AddRequiredSkill(skillId, SkillLevel.Intermediate, isMandatory: true);

        var profile = NewProfile();
        profile.AddVerifiedSkill(skillId, SkillLevel.Advanced, "cert-001", "mgr");

        var (matched, missing, total) = SkillCoverageCalculator.Compute(req, profile);

        matched.Should().Be(1);
        missing.Should().Be(0);
        total.Should().Be(1);
    }

    [Fact]
    public void ExpertLevel_MatchesAnyRequirement()
    {
        var skillId = Guid.NewGuid();
        var req = NewRequirement();
        req.AddRequiredSkill(skillId, SkillLevel.Novice, isMandatory: true);

        var profile = NewProfile();
        profile.AddVerifiedSkill(skillId, SkillLevel.Expert, "cert-001", "mgr");

        var (matched, _, _) = SkillCoverageCalculator.Compute(req, profile);

        matched.Should().Be(1);
    }

    // --- Boundary: zero and full coverage ---

    [Fact]
    public void NoRequiredSkills_ReturnsZeroTotalAndZeroMissing()
    {
        var req = NewRequirement();
        var profile = NewProfile();

        var (matched, missing, total) = SkillCoverageCalculator.Compute(req, profile);

        matched.Should().Be(0);
        missing.Should().Be(0);
        total.Should().Be(0);
    }

    [Fact]
    public void AllSkillsMet_ReturnsFullCoverage()
    {
        var skillA = Guid.NewGuid();
        var skillB = Guid.NewGuid();
        var req = NewRequirement();
        req.AddRequiredSkill(skillA, SkillLevel.Intermediate, isMandatory: true);
        req.AddRequiredSkill(skillB, SkillLevel.Advanced, isMandatory: true);

        var profile = NewProfile();
        profile.AddVerifiedSkill(skillA, SkillLevel.Advanced, "cert-001", "mgr");
        profile.AddVerifiedSkill(skillB, SkillLevel.Expert, "cert-002", "mgr");

        var (matched, missing, total) = SkillCoverageCalculator.Compute(req, profile);

        matched.Should().Be(2);
        missing.Should().Be(0);
        total.Should().Be(2);
    }

    [Fact]
    public void NoSkillsMet_ReturnsZeroMatched()
    {
        var skillA = Guid.NewGuid();
        var skillB = Guid.NewGuid();
        var req = NewRequirement();
        req.AddRequiredSkill(skillA, SkillLevel.Advanced, isMandatory: true);
        req.AddRequiredSkill(skillB, SkillLevel.Expert, isMandatory: true);

        var profile = NewProfile();
        profile.AddVerifiedSkill(skillA, SkillLevel.Novice, "cert-001", "mgr");
        // skillB completely absent

        var (matched, missing, total) = SkillCoverageCalculator.Compute(req, profile);

        matched.Should().Be(0);
        missing.Should().Be(2);
        total.Should().Be(2);
    }

    [Fact]
    public void PartialCoverage_CountsCorrectly()
    {
        var skillA = Guid.NewGuid();
        var skillB = Guid.NewGuid();
        var skillC = Guid.NewGuid();
        var req = NewRequirement();
        req.AddRequiredSkill(skillA, SkillLevel.Intermediate, isMandatory: true);
        req.AddRequiredSkill(skillB, SkillLevel.Intermediate, isMandatory: true);
        req.AddRequiredSkill(skillC, SkillLevel.Intermediate, isMandatory: true);

        var profile = NewProfile();
        profile.AddVerifiedSkill(skillA, SkillLevel.Advanced, "cert-001", "mgr");  // met
        profile.AddVerifiedSkill(skillB, SkillLevel.Novice, "cert-002", "mgr");    // not met
        // skillC absent                                                             // missing

        var (matched, missing, total) = SkillCoverageCalculator.Compute(req, profile);

        matched.Should().Be(1);
        missing.Should().Be(2);
        total.Should().Be(3);
    }

    // --- Optional skills excluded ---

    [Fact]
    public void OptionalSkills_NotCountedInTotal()
    {
        var mandatorySkill = Guid.NewGuid();
        var optionalSkill = Guid.NewGuid();
        var req = NewRequirement();
        req.AddRequiredSkill(mandatorySkill, SkillLevel.Intermediate, isMandatory: true);
        req.AddRequiredSkill(optionalSkill, SkillLevel.Advanced, isMandatory: false);

        var profile = NewProfile();
        // Only mandatory skill verified
        profile.AddVerifiedSkill(mandatorySkill, SkillLevel.Intermediate, "cert-001", "mgr");

        var (matched, missing, total) = SkillCoverageCalculator.Compute(req, profile);

        total.Should().Be(1, "optional skills are not counted");
        matched.Should().Be(1);
        missing.Should().Be(0);
    }

    [Fact]
    public void OptionalSkillMet_DoesNotInflateCoverage()
    {
        var mandatorySkill = Guid.NewGuid();
        var optionalSkill = Guid.NewGuid();
        var req = NewRequirement();
        req.AddRequiredSkill(mandatorySkill, SkillLevel.Expert, isMandatory: true);   // not met
        req.AddRequiredSkill(optionalSkill, SkillLevel.Novice, isMandatory: false);

        var profile = NewProfile();
        profile.AddVerifiedSkill(mandatorySkill, SkillLevel.Novice, "cert-001", "mgr");    // below
        profile.AddVerifiedSkill(optionalSkill, SkillLevel.Expert, "cert-002", "mgr");     // met, but optional

        var (matched, missing, total) = SkillCoverageCalculator.Compute(req, profile);

        total.Should().Be(1);
        matched.Should().Be(0);
        missing.Should().Be(1);
    }

    // --- Missing skill (not in profile at all) ---

    [Fact]
    public void SkillMissingFromProfile_CountedAsMissing()
    {
        var skillId = Guid.NewGuid();
        var req = NewRequirement();
        req.AddRequiredSkill(skillId, SkillLevel.Intermediate, isMandatory: true);

        var profile = NewProfile();
        // skill not added at all

        var (matched, missing, total) = SkillCoverageCalculator.Compute(req, profile);

        matched.Should().Be(0);
        missing.Should().Be(1);
        total.Should().Be(1);
    }
}
