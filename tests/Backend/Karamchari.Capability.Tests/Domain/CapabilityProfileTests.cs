using FluentAssertions;
using Karamchari.Capability.Domain.Primitives;
using Karamchari.Capability.Domain.Skills;
using Xunit;

namespace Karamchari.Capability.Tests.Domain;

public class CapabilityProfileTests
{
    private static readonly string TenantId = "tenant-1";
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid SkillId = Guid.NewGuid();

    private static CapabilityProfile NewProfile()
        => CapabilityProfile.Initialize(TenantId, EmployeeId);

    [Fact]
    public void CapabilityProfile_Initialize_HasNoSkills()
    {
        var profile = NewProfile();

        profile.Skills.Should().BeEmpty();
        profile.TenantId.Should().Be(TenantId);
        profile.EmployeeId.Should().Be(EmployeeId);
    }

    [Fact]
    public void CapabilityProfile_Initialize_GeneratesId()
    {
        var profile = NewProfile();
        profile.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void CapabilityProfile_AddVerifiedSkill_WithEvidence_AddsSkill()
    {
        var profile = NewProfile();

        profile.AddVerifiedSkill(SkillId, SkillLevel.Intermediate, "cert-001", "manager@example.com");

        profile.Skills.Should().HaveCount(1);
        var skill = profile.Skills.Single();
        skill.SkillId.Should().Be(SkillId);
        skill.Level.Should().Be(SkillLevel.Intermediate);
        skill.EvidenceReference.Should().Be("cert-001");
        skill.VerifiedBy.Should().Be("manager@example.com");
    }

    [Fact]
    public void CapabilityProfile_AddVerifiedSkill_WithoutEvidence_ThrowsDomainException()
    {
        var profile = NewProfile();

        var act = () => profile.AddVerifiedSkill(SkillId, SkillLevel.Novice, string.Empty, "manager@example.com");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*evidence*");
    }

    [Fact]
    public void CapabilityProfile_AddVerifiedSkill_NullEvidence_ThrowsDomainException()
    {
        var profile = NewProfile();

        var act = () => profile.AddVerifiedSkill(SkillId, SkillLevel.Novice, null!, "manager@example.com");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CapabilityProfile_AddVerifiedSkill_DuplicateSkill_ThrowsDomainException()
    {
        var profile = NewProfile();
        profile.AddVerifiedSkill(SkillId, SkillLevel.Novice, "cert-001", "manager@example.com");

        var act = () => profile.AddVerifiedSkill(SkillId, SkillLevel.Intermediate, "cert-002", "manager@example.com");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already mapped*");
    }

    [Fact]
    public void CapabilityProfile_UpdateSkillLevel_ChangesLevelAndRequiresEvidence()
    {
        var profile = NewProfile();
        profile.AddVerifiedSkill(SkillId, SkillLevel.Novice, "cert-001", "manager@example.com");

        profile.UpdateSkillLevel(SkillId, SkillLevel.Advanced, "cert-002-advanced", "senior@example.com");

        var skill = profile.Skills.Single();
        skill.Level.Should().Be(SkillLevel.Advanced);
        skill.EvidenceReference.Should().Be("cert-002-advanced");
        skill.VerifiedBy.Should().Be("senior@example.com");
    }

    [Fact]
    public void CapabilityProfile_UpdateSkillLevel_WithoutEvidence_ThrowsDomainException()
    {
        var profile = NewProfile();
        profile.AddVerifiedSkill(SkillId, SkillLevel.Novice, "cert-001", "manager@example.com");

        var act = () => profile.UpdateSkillLevel(SkillId, SkillLevel.Expert, string.Empty, "manager@example.com");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CapabilityProfile_UpdateSkillLevel_SkillNotFound_ThrowsInvalidOperationException()
    {
        var profile = NewProfile();

        var act = () => profile.UpdateSkillLevel(Guid.NewGuid(), SkillLevel.Expert, "cert-X", "manager@example.com");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void CapabilityProfile_AddVerifiedSkill_UpdatesLastUpdatedUtc()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var profile = NewProfile();

        profile.AddVerifiedSkill(SkillId, SkillLevel.Intermediate, "cert-001", "mgr@example.com");

        profile.LastUpdatedUtc.Should().NotBeNull();
        profile.LastUpdatedUtc!.Value.Should().BeAfter(before);
    }
}
