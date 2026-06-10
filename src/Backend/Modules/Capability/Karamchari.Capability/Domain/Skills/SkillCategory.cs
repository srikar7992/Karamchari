// -----------------------------------------------------------------------
// <copyright file="SkillCategory.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Skills;

/// <summary>
/// Represents a governed skill category in the organizational taxonomy.
/// Supports optional parent for two-level hierarchy (e.g. Technical > Cloud).
/// </summary>
public sealed class SkillCategory : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid? ParentCategoryId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private SkillCategory() { }

    public static SkillCategory Create(string tenantId, string name, string description, Guid? parentCategoryId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(description);

        return new SkillCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description,
            ParentCategoryId = parentCategoryId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = description;
    }

    public void Deactivate() => IsActive = false;
}
