// -----------------------------------------------------------------------
// <copyright file="Skill.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Skills;

/// <summary>
/// A skill definition within a SkillCategory + SkillTaxonomy.
/// ProficiencyDescriptors define behavioral anchors per level â€” stored as JSON.
/// </summary>
public sealed class Skill : Entity<Guid>, ITenantOwned
{
    private readonly List<SkillProficiencyDescriptor> _proficiencyDescriptors = [];

    private Skill() { /* EF materialization */ }

    internal Skill(
        Guid id,
        string tenantId,
        Guid categoryId,
        string name,
        string? description,
        SkillDomain domain,
        bool isActive) : base(id)
    {
        TenantId = tenantId;
        CategoryId = categoryId;
        Name = name;
        Description = description;
        Domain = domain;
        IsActive = isActive;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid CategoryId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public SkillDomain Domain { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsActive { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<SkillProficiencyDescriptor> ProficiencyDescriptors => _proficiencyDescriptors.AsReadOnly();

    internal void AddProficiencyDescriptor(SkillProficiencyDescriptor descriptor)
    {
        if (_proficiencyDescriptors.Any(d => d.Level == descriptor.Level))
            throw new InvalidOperationException($"Descriptor for level {descriptor.Level} already exists.");
        _proficiencyDescriptors.Add(descriptor);
    }

    internal void Deactivate() => IsActive = false;
}
