// -----------------------------------------------------------------------
// <copyright file="OnboardingTask.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Onboarding.Domain;

/// <summary>
/// A single actionable task within an onboarding case.
/// Owned by OnboardingCase aggregate — not accessed directly.
/// </summary>
public sealed class OnboardingTask : Entity<OnboardingTaskId>
{
    private OnboardingTask() { Title = string.Empty; }

    internal OnboardingTask(
        OnboardingTaskId id,
        OnboardingCaseId caseId,
        string title,
        TaskCategory category,
        string? assignedToEmployeeId,
        DateTimeOffset? dueDate,
        string? description) : base(id)
    {
        CaseId = caseId;
        Title = title;
        Category = category;
        AssignedToEmployeeId = assignedToEmployeeId;
        DueDate = dueDate;
        Description = description;
        Status = TaskStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public OnboardingCaseId CaseId { get; private set; } = null!;
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public TaskCategory Category { get; private set; }
    public string? AssignedToEmployeeId { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public TaskStatus Status { get; private set; }
    public Guid? CompletedByEmployeeId { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? CompletionNotes { get; private set; }
    public string? SkipReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    internal void Complete(Guid completedByEmployeeId, string? notes)
    {
        if (Status == TaskStatus.Completed)
            throw new InvalidOperationException("Task already completed.");
        if (Status == TaskStatus.Skipped)
            throw new InvalidOperationException("Task was skipped; cannot complete.");

        Status = TaskStatus.Completed;
        CompletedByEmployeeId = completedByEmployeeId;
        CompletedAt = DateTimeOffset.UtcNow;
        CompletionNotes = notes;
    }

    internal void Skip(string reason)
    {
        if (Status == TaskStatus.Completed)
            throw new InvalidOperationException("Cannot skip completed task.");
        Status = TaskStatus.Skipped;
        SkipReason = reason;
    }

    public bool IsOverdue =>
        Status == TaskStatus.Pending &&
        DueDate.HasValue &&
        DueDate.Value < DateTimeOffset.UtcNow;
}
