using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Engagement.Domain;

public record RecognitionId(Guid Value)
{
    public static RecognitionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public record BadgeId(Guid Value)
{
    public static BadgeId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Peer or manager recognition event — public praise with optional badge.
/// Surfaces on social wall and employee profile. LinkedIn-shareable certificates emitted via notifications.
/// </summary>
public sealed class EmployeeRecognition : AggregateRoot<RecognitionId>, ITenantOwned
{
    private readonly List<RecognitionReaction> _reactions = [];

    private EmployeeRecognition(
        RecognitionId id,
        string tenantId,
        Guid nominatorEmployeeId,
        Guid nomineeEmployeeId,
        BadgeId? badgeId,
        string message,
        bool isPublic)
        : base(id)
    {
        TenantId = tenantId;
        NominatorEmployeeId = nominatorEmployeeId;
        NomineeEmployeeId = nomineeEmployeeId;
        BadgeId = badgeId;
        Message = message;
        IsPublic = isPublic;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private EmployeeRecognition() { TenantId = string.Empty; Message = string.Empty; }

    public string TenantId { get; private set; }
    public Guid NominatorEmployeeId { get; private set; }
    public Guid NomineeEmployeeId { get; private set; }
    public BadgeId? BadgeId { get; private set; }
    public string Message { get; private set; }
    public bool IsPublic { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<RecognitionReaction> Reactions => _reactions.AsReadOnly();

    public static EmployeeRecognition Give(
        string tenantId,
        Guid nominatorId,
        Guid nomineeId,
        string message,
        BadgeId? badgeId = null,
        bool isPublic = true)
    {
        if (nominatorId == nomineeId) throw new ArgumentException("Cannot recognise yourself.");
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new EmployeeRecognition(RecognitionId.New(), tenantId, nominatorId, nomineeId, badgeId, message, isPublic);
    }

    public void React(Guid employeeId, string emoji)
    {
        var existing = _reactions.FirstOrDefault(r => r.EmployeeId == employeeId);
        if (existing != null) _reactions.Remove(existing);
        _reactions.Add(new RecognitionReaction(employeeId, emoji, DateTimeOffset.UtcNow));
    }
}

public sealed record RecognitionReaction(Guid EmployeeId, string Emoji, DateTimeOffset ReactedAt);

/// <summary>
/// Badge master — defines recognition badges available in tenant (e.g. "Team Player", "Innovator").
/// </summary>
public sealed class RecognitionBadge : AggregateRoot<BadgeId>, ITenantOwned
{
    private RecognitionBadge(BadgeId id, string tenantId, string name, string? iconUrl, string? description)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        IconUrl = iconUrl;
        Description = description;
        IsActive = true;
    }

    private RecognitionBadge() { TenantId = string.Empty; Name = string.Empty; }

    public string TenantId { get; private set; }
    public string Name { get; private set; }
    public string? IconUrl { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public static RecognitionBadge Create(string tenantId, string name, string? iconUrl = null, string? description = null) =>
        new(BadgeId.New(), tenantId, name, iconUrl, description);

    public void Deactivate() => IsActive = false;
}
