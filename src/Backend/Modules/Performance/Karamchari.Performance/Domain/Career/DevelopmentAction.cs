using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Career;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class DevelopmentAction : Entity<Guid>
{
    private DevelopmentAction() { /* EF materialization */ }

    internal DevelopmentAction(
        Guid id,
        Guid growthPlanId,
        string title,
        DevelopmentActionType type,
        string? description,
        DateOnly? targetCompletionDate) : base(id)
    {
        GrowthPlanId = growthPlanId;
        Title = title;
        Type = type;
        Description = description;
        TargetCompletionDate = targetCompletionDate;
        Status = DevelopmentActionStatus.NotStarted;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid GrowthPlanId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Title { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DevelopmentActionType Type { get; private set; }
    public string? Description { get; private set; }
    public DateOnly? TargetCompletionDate { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DevelopmentActionStatus Status { get; private set; }
    public string? CompletionNotes { get; private set; }
    public DateOnly? CompletedOnDate { get; private set; }

    internal void Start()
    {
        if (Status != DevelopmentActionStatus.NotStarted)
            throw new InvalidOperationException($"Cannot start from {Status}.");
        Status = DevelopmentActionStatus.InProgress;
    }

    internal void Complete(string? notes)
    {
        if (Status != DevelopmentActionStatus.InProgress)
            throw new InvalidOperationException($"Cannot complete from {Status}.");
        Status = DevelopmentActionStatus.Completed;
        CompletionNotes = notes?.Trim();
        CompletedOnDate = DateOnly.FromDateTime(DateTime.UtcNow);
    }

    internal void Defer() => Status = DevelopmentActionStatus.Deferred;
    internal void Cancel() => Status = DevelopmentActionStatus.Cancelled;
}
