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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
    /// <inheritdoc/>
    public DateTimeOffset? LastUpdatedUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<VerifiedSkill> Skills => _skills.AsReadOnly();

    private CapabilityProfile() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void AddVerifiedSkill(Guid skillId, SkillLevel level, string evidenceRef, string verifiedBy)
    {
        // Prevent duplicate skill entries; require an update instead if it exists
        if (_skills.Any(s => s.SkillId == skillId))
            throw new InvalidOperationException("Skill is already mapped to this profile. Use UpdateSkillLevel instead.");

        _skills.Add(VerifiedSkill.Create(Id, skillId, level, evidenceRef, verifiedBy));
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void UpdateSkillLevel(Guid skillId, SkillLevel newLevel, string newEvidenceRef, string verifiedBy)
    {
        var skill = _skills.FirstOrDefault(s => s.SkillId == skillId)
            ?? throw new InvalidOperationException("Skill not found on profile.");

        skill.UpdateLevel(newLevel, newEvidenceRef, verifiedBy);
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class VerifiedSkill : Entity<Guid>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ProfileId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid SkillId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public SkillLevel Level { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string EvidenceReference { get; private set; } = string.Empty; // URL, Certificate ID, or Assessment ID
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string VerifiedBy { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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
