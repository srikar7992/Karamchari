// -----------------------------------------------------------------------
// <copyright file="RoleSkillRequirement.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Capability.Domain.Primitives;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Skills;

/// <summary>
/// Defines the skills required for a specific role/position at minimum proficiency levels.
/// Used by SkillGapAnalysis to compute what an employee needs to grow into or qualify for.
/// </summary>
public sealed class RoleSkillRequirement : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<RequiredSkill> _skills = [];

    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Role title or position code this requirement applies to.</summary>
    public string RoleTitle { get; private set; } = string.Empty;

    /// <summary>Optional job family — groups related roles (e.g. "Warehouse Operations").</summary>
    public string? JobFamily { get; private set; }

    public string? Department { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyList<RequiredSkill> Skills => _skills.AsReadOnly();

    private RoleSkillRequirement() { }

    public static RoleSkillRequirement Define(
        string tenantId,
        string roleTitle,
        string? jobFamily,
        string? department)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleTitle);

        return new RoleSkillRequirement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RoleTitle = roleTitle.Trim(),
            JobFamily = jobFamily,
            Department = department,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void AddRequiredSkill(Guid skillId, SkillLevel minimumLevel, bool isMandatory = true)
    {
        if (_skills.Any(s => s.SkillId == skillId))
            throw new InvalidOperationException("Skill already listed in requirements.");
        _skills.Add(new RequiredSkill(Guid.NewGuid(), Id, skillId, minimumLevel, isMandatory));
    }

    public void RemoveSkill(Guid skillId)
    {
        var existing = _skills.FirstOrDefault(s => s.SkillId == skillId);
        if (existing is not null)
            _skills.Remove(existing);
    }

    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Compute skill gaps for an employee profile.
    /// Returns list of gaps: (SkillId, RequiredLevel, ActualLevel) where employee is below minimum.
    /// Missing skills are returned with ActualLevel = null.
    /// </summary>
    public IReadOnlyList<SkillGap> ComputeGaps(CapabilityProfile profile)
    {
        var gaps = new List<SkillGap>();

        foreach (var req in _skills.Where(s => s.IsMandatory))
        {
            var actual = profile.Skills.FirstOrDefault(s => s.SkillId == req.SkillId);
            if (actual is null || actual.Level < req.MinimumLevel)
            {
                gaps.Add(new SkillGap(
                    req.SkillId,
                    req.MinimumLevel,
                    actual?.Level));
            }
        }

        return gaps;
    }
}

public sealed class RequiredSkill : Entity<Guid>
{
    private RequiredSkill() { }

    internal RequiredSkill(
        Guid id,
        Guid requirementId,
        Guid skillId,
        SkillLevel minimumLevel,
        bool isMandatory) : base(id)
    {
        RequirementId = requirementId;
        SkillId = skillId;
        MinimumLevel = minimumLevel;
        IsMandatory = isMandatory;
    }

    public Guid RequirementId { get; private set; }
    public Guid SkillId { get; private set; }
    public SkillLevel MinimumLevel { get; private set; }
    public bool IsMandatory { get; private set; }
}

/// <summary>Read-only projection of a single skill gap — not persisted.</summary>
public sealed record SkillGap(
    Guid SkillId,
    SkillLevel Required,
    SkillLevel? Actual)
{
    public bool IsMissing => Actual is null;
    public int LevelsBehind => IsMissing ? (int)Required : (int)Required - (int)Actual!.Value;
}
