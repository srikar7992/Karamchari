using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Skills;

/// <summary>
/// Aggregate root representing a governed skill within the organizational taxonomy.
/// Prevents skill fragmentation (e.g. "C#" vs "C-Sharp").
/// </summary>
public sealed class SkillDefinition : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty; // e.g. "Technical", "Leadership"
    public string Description { get; private set; } = string.Empty;
    
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private SkillDefinition() { }

    public static SkillDefinition Define(string tenantId, string name, string category, string description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Skill name is required.");

        return new SkillDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Category = category.Trim(),
            Description = description,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void Deprecate()
    {
        IsActive = false;
    }
}
