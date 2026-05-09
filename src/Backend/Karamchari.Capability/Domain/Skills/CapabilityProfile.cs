using Karamchari.Capability.Domain.Primitives;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Skills;

/// <summary>
/// Aggregate root representing an employee's verified capabilities.
/// Endorsements without evidence are prohibited.
/// </summary>
public sealed class CapabilityProfile : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<VerifiedSkill> _skills = [];

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? LastUpdatedUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<VerifiedSkill> Skills => _skills.AsReadOnly();

    private CapabilityProfile() { }

    public static CapabilityProfile Initialize(string tenantId, Guid employeeId)
    {
        return new CapabilityProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void AddVerifiedSkill(Guid skillId, SkillLevel level, string evidenceRef, string verifiedBy)
    {
        // Prevent duplicate skill entries; require an update instead if it exists
        if (_skills.Any(s => s.SkillId == skillId))
            throw new InvalidOperationException("Skill is already mapped to this profile. Use UpdateSkillLevel instead.");

        _skills.Add(VerifiedSkill.Create(Id, skillId, level, evidenceRef, verifiedBy));
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }

    public void UpdateSkillLevel(Guid skillId, SkillLevel newLevel, string newEvidenceRef, string verifiedBy)
    {
        var skill = _skills.FirstOrDefault(s => s.SkillId == skillId)
            ?? throw new InvalidOperationException("Skill not found on profile.");

        skill.UpdateLevel(newLevel, newEvidenceRef, verifiedBy);
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }
}

public sealed class VerifiedSkill : Entity<Guid>
{
    public Guid ProfileId { get; private set; }
    public Guid SkillId { get; private set; }
    public SkillLevel Level { get; private set; }
    
    public string EvidenceReference { get; private set; } = string.Empty; // URL, Certificate ID, or Assessment ID
    public string VerifiedBy { get; private set; } = string.Empty;
    public DateTimeOffset VerifiedAtUtc { get; private set; }

    private VerifiedSkill() { }

    internal static VerifiedSkill Create(Guid profileId, Guid skillId, SkillLevel level, string evidence, string verifier)
    {
        if (string.IsNullOrWhiteSpace(evidence)) throw new ArgumentException("Skill evidence is mandatory.");

        return new VerifiedSkill
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            SkillId = skillId,
            Level = level,
            EvidenceReference = evidence,
            VerifiedBy = verifier,
            VerifiedAtUtc = DateTimeOffset.UtcNow
        };
    }

    internal void UpdateLevel(SkillLevel level, string evidence, string verifier)
    {
        if (string.IsNullOrWhiteSpace(evidence)) throw new ArgumentException("Skill evidence is mandatory.");

        Level = level;
        EvidenceReference = evidence;
        VerifiedBy = verifier;
        VerifiedAtUtc = DateTimeOffset.UtcNow;
    }
}
