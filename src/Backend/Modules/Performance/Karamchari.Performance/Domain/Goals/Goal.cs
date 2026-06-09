using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Goals.Events;

namespace Karamchari.Performance.Domain.Goals;

/// <summary>
/// Aggregate root representing a performance goal.
/// Business truth is owned here; coordination (approval) is managed by Workflow.
/// </summary>
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
    public GoalOwnerType OwnerType { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid OwnerId { get; private set; }
    public Guid? ParentGoalId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public GoalType Type { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TargetValue { get; private set; }
    public string? Unit { get; private set; }

    /// <summary>0â€“100. Sum of weights for a given owner in a cycle must equal 100.</summary>
    public decimal Weight { get; private set; }

    /// <summary>0â€“100 percentage-equivalent completion.</summary>
    public decimal CurrentProgress { get; private set; }

    /// <summary>Set at cycle lock by manager/system scoring.</summary>
    public decimal? FinalScore { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public GoalStatus Status { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<GoalProgressUpdate> Updates => _updates.AsReadOnly();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<GoalRevisionRecord> Revisions => _revisions.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be 0â€“100.");

        if (targetValue < 0)
            throw new ArgumentOutOfRangeException(nameof(targetValue), "TargetValue cannot be negative.");

        return new Goal(Guid.NewGuid(), tenantId, cycleId, ownerType, ownerId,
            parentGoalId, title.Trim(), description?.Trim(), type, targetValue, unit?.Trim(), weight);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Submit()
    {
        if (Status != GoalStatus.Draft)
            throw new InvalidOperationException($"Cannot submit goal from {Status}.");

        // Business state remains Draft until coordination completes.
    }

    /// <summary>
    /// Finalizes the goal as active, making it authoritative truth for tracking.
    /// Called by Workflow coordination upon successful completion.
    /// </summary>
    public void FinalizeApproved()
    {
        if (Status != GoalStatus.Draft)
            throw new InvalidOperationException($"Cannot activate goal from {Status}.");

        Status = GoalStatus.Active;
        RaiseDomainEvent(new GoalApproved(Id, TenantId, OwnerId, OwnerType, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Rejects the goal during coordination.
    /// </summary>
    public void FinalizeRejected()
    {
        if (Status != GoalStatus.Draft)
            throw new InvalidOperationException($"Cannot reject goal from {Status}.");

        Status = GoalStatus.Rejected;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void RecordFinalScore(decimal score, Guid scoredBy)
    {
        if (score is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be 0â€“100.");

        FinalScore = score;
        Status = GoalStatus.Closed;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Align this goal to a parent goal, enforcing cycle consistency and level rules.
    /// </summary>
    public void AlignTo(Goal parentGoal)
    {
        ArgumentNullException.ThrowIfNull(parentGoal);

        if (parentGoal.CycleId != CycleId)
        {
            throw new InvalidOperationException("Cannot align to a goal in a different cycle.");
        }

        if (parentGoal.TenantId != TenantId)
        {
            throw new InvalidOperationException("Cannot align to a goal in a different tenant.");
        }

        // Hierarchy validation: child goal OwnerType must be lower or equal priority than parent goal OwnerType.
        // Organization (1) > Department (2) > Team (3) > Individual (4)
        if ((int)OwnerType < (int)parentGoal.OwnerType)
        {
            throw new InvalidOperationException($"Cannot align a higher-level goal ({OwnerType}) to a lower-level goal ({parentGoal.OwnerType}).");
        }

        ParentGoalId = parentGoal.Id;
    }
}
