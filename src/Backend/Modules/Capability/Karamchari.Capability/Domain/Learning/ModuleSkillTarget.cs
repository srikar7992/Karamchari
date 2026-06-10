// -----------------------------------------------------------------------
// <copyright file="ModuleSkillTarget.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Capability.Domain.Primitives;
using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Capability.Domain.Learning;

/// <summary>
/// Maps a LearningModule to a skill it develops, at the level granted upon completion.
/// Owned by LearningModule.
/// </summary>
public sealed class ModuleSkillTarget : Entity<Guid>
{
    public Guid ModuleId { get; private set; }
    public Guid SkillId { get; private set; }

    /// <summary>Skill level an employee receives upon completing this module.</summary>
    public SkillLevel GrantedLevel { get; private set; }

    private ModuleSkillTarget() { }

    internal static ModuleSkillTarget Create(Guid moduleId, Guid skillId, SkillLevel grantedLevel)
    {
        return new ModuleSkillTarget
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            SkillId = skillId,
            GrantedLevel = grantedLevel
        };
    }
}
