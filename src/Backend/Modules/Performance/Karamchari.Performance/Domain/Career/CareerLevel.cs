// -----------------------------------------------------------------------
// <copyright file="CareerLevel.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Career;

/// <summary>
/// One level within a CareerTrack (e.g., L3, Senior, Principal).
/// CompetencyRequirements define the skill bar for this level â€” stored as JSON.
/// </summary>
public sealed class CareerLevel : Entity<Guid>
{
    private readonly List<CompetencyRequirement> _competencyRequirements = [];

    private CareerLevel() { /* EF materialization */ }

    internal CareerLevel(
        Guid id,
        Guid trackId,
        string code,
        string displayName,
        int order,
        string? description) : base(id)
    {
        TrackId = trackId;
        Code = code;
        DisplayName = displayName;
        Order = order;
        Description = description;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid TrackId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Code { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int Order { get; private set; }
    public string? Description { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<CompetencyRequirement> CompetencyRequirements => _competencyRequirements.AsReadOnly();

    internal void AddCompetencyRequirement(CompetencyRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        if (_competencyRequirements.Any(r => r.SkillId == requirement.SkillId))
            throw new InvalidOperationException($"Competency for skill {requirement.SkillId} already defined at this level.");
        _competencyRequirements.Add(requirement);
    }
}
