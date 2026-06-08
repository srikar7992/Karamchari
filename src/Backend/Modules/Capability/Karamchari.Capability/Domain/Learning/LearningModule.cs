using Karamchari.Capability.Domain.Primitives;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Learning;

/// <summary>
/// Aggregate root representing a structured learning course or assessment.
/// Can link to external SCORM/LMS systems.
/// </summary>
public sealed class LearningModule : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ModuleSkillTarget> _skillTargets = [];
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Title { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Description { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int EstimatedMinutes { get; private set; }

    /// <inheritdoc/>
    public string? ExternalProviderId { get; private set; } // e.g. LinkedIn Learning Course ID
    /// <inheritdoc/>
    public string? ContentUrl { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsActive { get; private set; } = true;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyList<ModuleSkillTarget> SkillTargets => _skillTargets.AsReadOnly();

    private LearningModule() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static LearningModule Create(
        string tenantId,
        string title,
        string desc,
        int minutes,
        string? providerId = null,
        string? url = null)
    {
        return new LearningModule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            Description = desc,
            EstimatedMinutes = minutes,
            ExternalProviderId = providerId,
            ContentUrl = url,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void LinkSkill(Guid skillId, SkillLevel grantedLevel)
    {
        if (_skillTargets.Any(t => t.SkillId == skillId))
            throw new InvalidOperationException("Skill already linked to this module.");

        _skillTargets.Add(ModuleSkillTarget.Create(Id, skillId, grantedLevel));
    }

    public void RemoveSkillLink(Guid skillId)
    {
        var target = _skillTargets.FirstOrDefault(t => t.SkillId == skillId);
        if (target is not null) _skillTargets.Remove(target);
    }

    public void Deactivate() => IsActive = false;
}
