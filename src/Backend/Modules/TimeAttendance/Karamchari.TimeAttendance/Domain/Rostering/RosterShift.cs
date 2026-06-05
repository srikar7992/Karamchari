using Karamchari.Core.Domain.Primitives;

namespace Karamchari.TimeAttendance.Domain.Rostering;

/// <summary>
/// A concrete shift instance within a roster — specific date, location, staffing requirements.
/// Unlike ShiftDefinition (reusable template), RosterShift is bound to a roster period and date.
/// Assignments reference this, not a template, so coverage validation has all data it needs.
/// </summary>
public sealed class RosterShift : Entity<Guid>
{
    private readonly List<SkillRequirement> _requiredSkills = [];

    private RosterShift() { }

    public Guid RosterId { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ShiftType Type { get; private set; }
    public DateOnly WorkDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public Guid LocationId { get; private set; }
    public int RequiredHeadcount { get; private set; }

    public IReadOnlyList<SkillRequirement> RequiredSkills => _requiredSkills.AsReadOnly();

    public bool IsOvernight => EndTime < StartTime;

    public TimeSpan Duration => IsOvernight
        ? TimeSpan.FromHours(24) - StartTime.ToTimeSpan() + EndTime.ToTimeSpan()
        : EndTime.ToTimeSpan() - StartTime.ToTimeSpan();

    internal static RosterShift Create(
        Guid rosterId,
        string tenantId,
        string name,
        ShiftType type,
        DateOnly workDate,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid locationId,
        int requiredHeadcount,
        IEnumerable<SkillRequirement>? requiredSkills = null)
    {
        if (requiredHeadcount < 1)
            throw new ArgumentOutOfRangeException(nameof(requiredHeadcount), "Must be at least 1.");

        var shift = new RosterShift
        {
            Id = Guid.NewGuid(),
            RosterId = rosterId,
            TenantId = tenantId,
            Name = name.Trim(),
            Type = type,
            WorkDate = workDate,
            StartTime = startTime,
            EndTime = endTime,
            LocationId = locationId,
            RequiredHeadcount = requiredHeadcount,
        };

        if (requiredSkills is not null)
            shift._requiredSkills.AddRange(requiredSkills);

        return shift;
    }

    internal void UpdateRequirements(int headcount, IEnumerable<SkillRequirement>? skills)
    {
        RequiredHeadcount = headcount >= 1 ? headcount : RequiredHeadcount;
        if (skills is not null)
        {
            _requiredSkills.Clear();
            _requiredSkills.AddRange(skills);
        }
    }
}
