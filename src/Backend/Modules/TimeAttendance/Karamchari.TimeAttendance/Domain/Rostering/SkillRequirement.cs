namespace Karamchari.TimeAttendance.Domain.Rostering;

public sealed record SkillRequirement(
    Guid SkillId,
    string SkillName,
    int MinimumLevel,
    int Count);
