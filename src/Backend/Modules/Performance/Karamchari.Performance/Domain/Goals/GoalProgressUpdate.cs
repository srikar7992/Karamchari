using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Goals;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid GoalId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Value { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal ProgressPercent { get; private set; }
    public string? Notes { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid UpdatedBy { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; private set; }
}
