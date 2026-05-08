using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Scoring;

namespace Karamchari.Performance.Domain.OKRs;

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

    public string TenantId { get; private set; } = string.Empty;
    public Guid CycleId { get; private set; }
    public ObjectiveOwnerType OwnerType { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid? ParentObjectiveId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsStretch { get; private set; }
    public ObjectiveStatus Status { get; private set; }

    /// <summary>Weighted average of KR scores × weights. 0–1.</summary>
    public decimal AggregatedScore { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<KeyResult> KeyResults => _keyResults.AsReadOnly();

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

    public void ComputeScore(IOKRScoringStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        foreach (var kr in _keyResults)
            kr.ComputeScore();

        AggregatedScore = strategy.ComputeObjectiveScore(_keyResults);
        Status = ObjectiveStatus.Scored;
    }

    public void Activate()
    {
        if (Status != ObjectiveStatus.Draft)
            throw new InvalidOperationException($"Cannot activate objective in state {Status}.");
        Status = ObjectiveStatus.Active;
    }
}
