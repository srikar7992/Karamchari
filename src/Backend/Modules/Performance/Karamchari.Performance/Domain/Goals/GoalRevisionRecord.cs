// -----------------------------------------------------------------------
// <copyright file="GoalRevisionRecord.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Goals;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid GoalId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string PreviousTitle { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal PreviousTarget { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string NewTitle { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal NewTarget { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Reason { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RevisedBy { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; private set; }
}
