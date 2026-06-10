// -----------------------------------------------------------------------
// <copyright file="SkillTaxonomy.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Skills;

/// <summary>
/// Root of the skill hierarchy: Taxonomy â†’ Category â†’ Skill â†’ ProficiencyDescriptors.
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsActive { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<SkillCategory> Categories => _categories.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static SkillTaxonomy Create(string tenantId, string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new SkillTaxonomy(Guid.NewGuid(), tenantId, name.Trim(), description?.Trim());
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public SkillCategory AddCategory(string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_categories.Any(c => c.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Category '{name}' already exists.");

        var category = new SkillCategory(Guid.NewGuid(), TenantId, Id, name.Trim(), description?.Trim());
        _categories.Add(category);
        return category;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Deactivate() => IsActive = false;
}
