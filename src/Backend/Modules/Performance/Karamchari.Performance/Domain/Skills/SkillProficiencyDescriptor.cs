// -----------------------------------------------------------------------
// <copyright file="SkillProficiencyDescriptor.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Skills;

/// <summary>
/// Describes what a proficiency level means for a specific skill.
/// Stored as owned collection on Skill (JSON column).
/// </summary>
public sealed record SkillProficiencyDescriptor(
    ProficiencyLevel Level,
    string Description,
    string? BehavioralIndicators);
