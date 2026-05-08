using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Goals.Events;

namespace Karamchari.Performance.Domain.Goals;

public sealed class Goal : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<GoalProgressUpdate> _updates = [];
    private readonly List<GoalRevisionRecord> _revisions = [];

    private Goal() { /* EF materialization */ }

    private Goal(
        Guid id,
        string tenantId,
        Guid cycleId,
        GoalOwnerType ownerType,
        Guid ownerId,
        Guid? parentGoalId,
        string title,
        string? description,
        GoalType type,
        decimal targetValue,
        string? unit,
        decimal weight) : base(id)
    {
        TenantId = tenantId;
        CycleId = cycleId;
        OwnerType = ownerType;
        OwnerId = ownerId;
        ParentGoalId = parentGoalId;
        Title = title;
        Description = description;
        Type = type;
        TargetValue = targetValue;
        Unit = unit;
        Weight = weight;
        Status = GoalStatus.Draft;
        CurrentProgress = 0m;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid CycleId { get; private set; }
    public GoalOwnerType OwnerType { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid? ParentGoalId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public GoalType Type { get; private set; }
    public decimal TargetValue { get; private set; }
    public string? Unit { get; private set; }

    /// <summary>0–100. Sum of weights for a given owner in a cycle must equal 100.</summary>
    public decimal Weight { get; private set; }

    /// <summary>0–100 percentage-equivalent completion.</summary>
    public decimal CurrentProgress { get; private set; }

    /// <summary>Set at cycle lock by manager/system scoring.</summary>
    public decimal? FinalScore { get; private set; }
    public GoalStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<GoalProgressUpdate> Updates => _updates.AsReadOnly();
    public IReadOnlyCollection<GoalRevisionRecord> Revisions => _revisions.AsReadOnly();

    public static Goal Create(
        string tenantId,
        Guid cycleId,
        GoalOwnerType ownerType,
        Guid ownerId,
        string title,
        GoalType type,
        decimal targetValue,
        string? unit,
        decimal weight,
        string? description = null,
        Guid? parentGoalId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (weight is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be 0–100.");

        if (targetValue < 0)
            throw new ArgumentOutOfRangeException(nameof(targetValue), "TargetValue cannot be negative.");

        return new Goal(Guid.NewGuid(), tenantId, cycleId, ownerType, ownerId,
            parentGoalId, title.Trim(), description?.Trim(), type, targetValue, unit?.Trim(), weight);
    }

    public void SubmitForApproval()
    {
        if (Status != GoalStatus.Draft)
            throw new InvalidOperationException($"Cannot submit goal in state {Status}.");

        Status = GoalStatus.PendingApproval;
    }

    public void Approve()
    {
        if (Status != GoalStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve goal in state {Status}.");

        Status = GoalStatus.Active;
        RaiseDomainEvent(new GoalApproved(Id, TenantId, OwnerId, OwnerType, DateTimeOffset.UtcNow));
    }

    public void Reject()
    {
        if (Status != GoalStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot reject goal in state {Status}.");

        Status = GoalStatus.Rejected;
    }

    public void UpdateProgress(decimal value, string? notes, Guid updatedBy)
    {
        if (Status != GoalStatus.Active)
            throw new InvalidOperationException($"Cannot update progress on goal in state {Status}.");

        var percent = TargetValue == 0
            ? 0m
            : Math.Min(Math.Round(value / TargetValue * 100m, 2), 100m);

        var update = new GoalProgressUpdate(
            Guid.NewGuid(), Id, value, percent, notes, updatedBy);

        _updates.Add(update);
        CurrentProgress = percent;
    }

    public void RecordFinalScore(decimal score, Guid scoredBy)
    {
        if (score is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be 0–100.");

        FinalScore = score;
        Status = GoalStatus.Closed;
    }

    public void Revise(string newTitle, decimal newTarget, string reason, Guid revisedBy)
    {
        if (Status != GoalStatus.Active)
            throw new InvalidOperationException($"Cannot revise goal in state {Status}.");

        ArgumentException.ThrowIfNullOrWhiteSpace(newTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var revision = new GoalRevisionRecord(
            Guid.NewGuid(), Id,
            Title, TargetValue,
            newTitle.Trim(), newTarget,
            reason.Trim(), revisedBy);

        _revisions.Add(revision);
        Title = newTitle.Trim();
        TargetValue = newTarget;
        Status = GoalStatus.Revised;

        RaiseDomainEvent(new GoalRevised(Id, TenantId, newTitle.Trim(), newTarget, reason.Trim(), revisedBy, DateTimeOffset.UtcNow));
    }
}
