using Karamchari.Performance.Domain.Skills;

namespace Karamchari.Performance.Domain.Career;

/// <summary>
/// Value object: minimum proficiency required for a skill at a given CareerLevel.
/// Stored as JSON collection on CareerLevel.
/// </summary>
public sealed record CompetencyRequirement(
    Guid SkillId,
    string SkillCode,
    ProficiencyLevel MinimumProficiency,
    bool IsMandatory);
