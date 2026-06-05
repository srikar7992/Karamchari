using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Rostering;

public sealed class CoverageRule : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<SkillRequirement> _skillMix = [];

    private CoverageRule() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public Guid LocationId { get; private set; }
    public string LocationName { get; private set; } = string.Empty;
    public int MinimumStaff { get; private set; }
    public int MaximumStaff { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<SkillRequirement> SkillMix => _skillMix.AsReadOnly();

    public static CoverageRule Create(
        string tenantId,
        Guid locationId,
        string locationName,
        int minimumStaff,
        int maximumStaff,
        IEnumerable<SkillRequirement>? skillMix = null)
    {
        if (minimumStaff < 0) throw new ArgumentException("Minimum staff cannot be negative.");
        if (maximumStaff < minimumStaff) throw new ArgumentException("Maximum staff must be >= minimum.");

        var rule = new CoverageRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = locationId,
            LocationName = locationName.Trim(),
            MinimumStaff = minimumStaff,
            MaximumStaff = maximumStaff,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        if (skillMix != null)
            rule._skillMix.AddRange(skillMix);

        return rule;
    }

    public void UpdateStaffing(int min, int max)
    {
        if (min < 0) throw new ArgumentException("Minimum staff cannot be negative.");
        if (max < min) throw new ArgumentException("Maximum staff must be >= minimum.");
        MinimumStaff = min;
        MaximumStaff = max;
    }

    public void SetSkillMix(IEnumerable<SkillRequirement> requirements)
    {
        _skillMix.Clear();
        _skillMix.AddRange(requirements);
    }

    public void Deactivate() => IsActive = false;
}
