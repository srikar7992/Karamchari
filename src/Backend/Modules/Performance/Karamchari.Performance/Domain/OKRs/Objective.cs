using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Scoring;

namespace Karamchari.Performance.Domain.OKRs;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class Objective : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<KeyResult> _keyResults = [];
    private Objective() { /* EF materialization */ }

    private Objective(
        Guid id,
        string tenantId,
        Guid cycleId,
        ObjectiveOwnerType ownerType,
        Guid ownerId,
        Guid? parentObjectiveId,
        string title,
        string? description,
        bool isStretch) : base(id)
    {
        TenantId = tenantId;
        CycleId = cycleId;
        OwnerType = ownerType;
        OwnerId = ownerId;
        ParentObjectiveId = parentObjectiveId;
        Title = title;
        Description = description;
        IsStretch = isStretch;
        Status = ObjectiveStatus.Draft;
        AggregatedScore = 0m;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid CycleId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ObjectiveOwnerType OwnerType { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid OwnerId { get; private set; }
    public Guid? ParentObjectiveId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsStretch { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ObjectiveStatus Status { get; private set; }

    /// <summary>Weighted average of KR scores Ã— weights. 0â€“1.</summary>
    public decimal AggregatedScore { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<KeyResult> KeyResults => _keyResults.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static Objective Create(
        string tenantId,
        Guid cycleId,
        ObjectiveOwnerType ownerType,
        Guid ownerId,
        string title,
        string? description = null,
        bool isStretch = false,
        Guid? parentObjectiveId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new Objective(Guid.NewGuid(), tenantId, cycleId, ownerType, ownerId,
            parentObjectiveId, title.Trim(), description?.Trim(), isStretch);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public KeyResult AddKeyResult(
        string title,
        KRType type,
        decimal target,
        string unit,
        decimal weight,
        Guid? dependsOnKrId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);

        if (weight is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(weight));

        var kr = new KeyResult(Guid.NewGuid(), Id, title.Trim(), type, target, unit.Trim(), weight, dependsOnKrId);
        _keyResults.Add(kr);
        return kr;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void RecordCheckIn(
        Guid krId,
        decimal value,
        decimal confidenceLevel,
        string? notes,
        Guid updatedBy)
    {
        var kr = _keyResults.FirstOrDefault(k => k.Id == krId)
            ?? throw new InvalidOperationException($"KeyResult {krId} not found on Objective {Id}.");

        kr.RecordCheckIn(value, confidenceLevel, notes, updatedBy);

        Status = _keyResults.Any(k => k.Status == KRStatus.OffTrack)
            ? ObjectiveStatus.OffTrack
            : _keyResults.Any(k => k.Status == KRStatus.AtRisk)
                ? ObjectiveStatus.AtRisk
                : ObjectiveStatus.Active;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void ComputeScore(IOKRScoringStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        foreach (var kr in _keyResults)
            kr.ComputeScore();

        AggregatedScore = strategy.ComputeObjectiveScore(_keyResults);
        Status = ObjectiveStatus.Scored;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Activate()
    {
        if (Status != ObjectiveStatus.Draft)
            throw new InvalidOperationException($"Cannot activate objective in state {Status}.");
        Status = ObjectiveStatus.Active;
    }
}
