// -----------------------------------------------------------------------
// <copyright file="EmployeeSkillGapProjection.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Karamchari.Capability.Domain.Primitives;

namespace Karamchari.Capability.Domain.Skills;

/// <summary>
/// Pre-computed per-skill gap for one employee against one role skill requirement.
/// A row exists only when the employee has a gap (missing or below required level).
/// When the gap is closed (skill validated at required level), the row is deleted.
/// PK: (TenantId, EmployeeId, RoleRequirementId, SkillId).
/// </summary>
public sealed class EmployeeSkillGapProjection
{
    private EmployeeSkillGapProjection() { }

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the employee identifier.</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>Gets the role skill requirement identifier this gap was computed against.</summary>
    public Guid RoleRequirementId { get; private set; }

    /// <summary>Gets the skill identifier where the gap exists.</summary>
    public Guid SkillId { get; private set; }

    /// <summary>Gets the role title from the requirement at the time this projection was computed.</summary>
    public string RoleTitle { get; private set; } = string.Empty;

    /// <summary>Gets the minimum level required by the role for this skill.</summary>
    public SkillLevel RequiredLevel { get; private set; }

    /// <summary>Gets the employee's current validated level, or null if skill is missing entirely.</summary>
    public SkillLevel? CurrentLevel { get; private set; }

    /// <summary>Gets the number of levels the employee must advance to meet the requirement.</summary>
    public int LevelsBehind { get; private set; }

    /// <summary>Gets whether the employee has no verified evidence for this skill at all.</summary>
    public bool IsMissing { get; private set; }

    /// <summary>Gets the UTC timestamp when this gap was last recomputed.</summary>
    public DateTimeOffset LastUpdatedUtc { get; private set; }

    /// <summary>Creates a new gap row from a computed <see cref="SkillGap"/>.</summary>
    public static EmployeeSkillGapProjection FromGap(
        string tenantId,
        Guid employeeId,
        Guid roleRequirementId,
        string roleTitle,
        SkillGap gap)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(gap);

        return new EmployeeSkillGapProjection
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            RoleRequirementId = roleRequirementId,
            SkillId = gap.SkillId,
            RoleTitle = roleTitle,
            RequiredLevel = gap.Required,
            CurrentLevel = gap.Actual,
            LevelsBehind = gap.LevelsBehind,
            IsMissing = gap.IsMissing,
            LastUpdatedUtc = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Updates this gap row in-place when the gap still exists but changed.</summary>
    public void Update(string roleTitle, SkillGap gap)
    {
        ArgumentNullException.ThrowIfNull(gap);

        RoleTitle = roleTitle;
        RequiredLevel = gap.Required;
        CurrentLevel = gap.Actual;
        LevelsBehind = gap.LevelsBehind;
        IsMissing = gap.IsMissing;
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }
}
