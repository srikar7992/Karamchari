using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Goals;

public sealed class GoalProgressUpdate : Entity<Guid>
{
    private GoalProgressUpdate() { /* EF materialization */ }

    internal GoalProgressUpdate(
        Guid id,
        Guid goalId,
        decimal value,
        decimal progressPercent,
        string? notes,
        Guid updatedBy) : base(id)
    {
        GoalId = goalId;
        Value = value;
        ProgressPercent = progressPercent;
        Notes = notes;
        UpdatedBy = updatedBy;
        OccurredOnUtc = DateTimeOffset.UtcNow;
    }

    public Guid GoalId { get; private set; }
    public decimal Value { get; private set; }
    public decimal ProgressPercent { get; private set; }
    public string? Notes { get; private set; }
    public Guid UpdatedBy { get; private set; }
    public DateTimeOffset OccurredOnUtc { get; private set; }
}
