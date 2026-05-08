using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Goals;

public sealed class GoalRevisionRecord : Entity<Guid>
{
    private GoalRevisionRecord() { /* EF materialization */ }

    internal GoalRevisionRecord(
        Guid id,
        Guid goalId,
        string previousTitle,
        decimal previousTarget,
        string newTitle,
        decimal newTarget,
        string reason,
        Guid revisedBy) : base(id)
    {
        GoalId = goalId;
        PreviousTitle = previousTitle;
        PreviousTarget = previousTarget;
        NewTitle = newTitle;
        NewTarget = newTarget;
        Reason = reason;
        RevisedBy = revisedBy;
        OccurredOnUtc = DateTimeOffset.UtcNow;
    }

    public Guid GoalId { get; private set; }
    public string PreviousTitle { get; private set; } = string.Empty;
    public decimal PreviousTarget { get; private set; }
    public string NewTitle { get; private set; } = string.Empty;
    public decimal NewTarget { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid RevisedBy { get; private set; }
    public DateTimeOffset OccurredOnUtc { get; private set; }
}
