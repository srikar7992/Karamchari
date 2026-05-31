using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Skills;

/// <summary>
/// Aggregate root representing a governed skill within the organizational taxonomy.
/// Prevents skill fragmentation (e.g. "C#" vs "C-Sharp").
/// </summary>
public sealed class SkillDefinition : AggregateRoot<Guid>, ITenantOwned
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Category { get; private set; } = string.Empty; // e.g. "Technical", "Leadership"
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsActive { get; private set; } = true;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private SkillDefinition() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static SkillDefinition Define(string tenantId, string name, string category, string description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Skill name is required.");
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(description);

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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Deprecate()
    {
        IsActive = false;
    }
}
