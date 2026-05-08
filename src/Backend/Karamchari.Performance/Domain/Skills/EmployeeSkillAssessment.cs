using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Skills;

/// <summary>
/// One assessment record for a specific skill, at a point in time.
/// Multiple assessments per skill accumulate — latest by AssessedOnUtc is current.
/// </summary>
public sealed class EmployeeSkillAssessment : Entity<Guid>
{
    private EmployeeSkillAssessment() { /* EF materialization */ }

    internal EmployeeSkillAssessment(
        Guid id,
        Guid profileId,
        Guid skillId,
        ProficiencyLevel level,
        AssessmentSource source,
        Guid? assessedByEmployeeId,
        string? evidence) : base(id)
    {
        ProfileId = profileId;
        SkillId = skillId;
        Level = level;
        Source = source;
        AssessedByEmployeeId = assessedByEmployeeId;
        Evidence = evidence;
        AssessedOnUtc = DateTimeOffset.UtcNow;
    }

    public Guid ProfileId { get; private set; }
    public Guid SkillId { get; private set; }
    public ProficiencyLevel Level { get; private set; }
    public AssessmentSource Source { get; private set; }
    public Guid? AssessedByEmployeeId { get; private set; }
    public string? Evidence { get; private set; }
    public DateTimeOffset AssessedOnUtc { get; private set; }
}
