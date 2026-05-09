using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Skills;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class SkillCategory : Entity<Guid>, ITenantOwned
{
    private readonly List<Skill> _skills = [];

    private SkillCategory() { /* EF materialization */ }

    internal SkillCategory(
        Guid id,
        string tenantId,
        Guid taxonomyId,
        string name,
        string? description) : base(id)
    {
        TenantId = tenantId;
        TaxonomyId = taxonomyId;
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid TaxonomyId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<Skill> Skills => _skills.AsReadOnly();

    internal Skill AddSkill(string name, string? description, SkillDomain domain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_skills.Any(s => s.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Skill '{name}' already exists in this category.");

        var skill = new Skill(Guid.NewGuid(), TenantId, Id, name.Trim(), description?.Trim(), domain, true);
        _skills.Add(skill);
        return skill;
    }
}
