using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Skills;

/// <summary>
/// Root of the skill hierarchy: Taxonomy → Category → Skill → ProficiencyDescriptors.
/// One active taxonomy per tenant (IsActive enforced at app layer, not domain).
/// </summary>
public sealed class SkillTaxonomy : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<SkillCategory> _categories = [];

    private SkillTaxonomy() { /* EF materialization */ }

    private SkillTaxonomy(Guid id, string tenantId, string name, string? description) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        IsActive = true;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<SkillCategory> Categories => _categories.AsReadOnly();

    public static SkillTaxonomy Create(string tenantId, string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new SkillTaxonomy(Guid.NewGuid(), tenantId, name.Trim(), description?.Trim());
    }

    public SkillCategory AddCategory(string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_categories.Any(c => c.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Category '{name}' already exists.");

        var category = new SkillCategory(Guid.NewGuid(), TenantId, Id, name.Trim(), description?.Trim());
        _categories.Add(category);
        return category;
    }

    public void Deactivate() => IsActive = false;
}
