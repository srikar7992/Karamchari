// -----------------------------------------------------------------------
// <copyright file="ReviewCycle.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Reviews.Events;

namespace Karamchari.Performance.Domain.Reviews;

/// <summary>
/// Drives one full review cycle from enrollment through calibration.
/// GoalCycleId / OKRCycleId are nullable foreign keys â€” links to those BCs
/// are advisory (cross-context join at query time, not enforced by FK).
/// </summary>
public sealed class ReviewCycle : AggregateRoot<Guid>, ITenantOwned
{
    private ReviewCycle() { /* EF materialization */ }

    private ReviewCycle(
        Guid id,
        string tenantId,
        string name,
        ReviewCycleType type,
        Guid templateId,
        DateOnly reviewPeriodStart,
        DateOnly reviewPeriodEnd,
        DateOnly submissionDeadline,
        Guid? goalCycleId,
        Guid? okrCycleId) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Type = type;
        TemplateId = templateId;
        ReviewPeriodStart = reviewPeriodStart;
        ReviewPeriodEnd = reviewPeriodEnd;
        SubmissionDeadline = submissionDeadline;
        GoalCycleId = goalCycleId;
        OKRCycleId = okrCycleId;
        Status = ReviewCycleStatus.Draft;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ReviewCycleType Type { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid TemplateId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly ReviewPeriodStart { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly ReviewPeriodEnd { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly SubmissionDeadline { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ReviewCycleStatus Status { get; private set; }

    /// <summary>Advisory link â€” cross-context. Never enforced as a DB FK across schemas.</summary>
    public Guid? GoalCycleId { get; private set; }

    /// <summary>Advisory link â€” cross-context. Never enforced as a DB FK across schemas.</summary>
    public Guid? OKRCycleId { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int TotalAssignments { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int CompletedAssignments { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static ReviewCycle Create(
        string tenantId,
        string name,
        ReviewCycleType type,
        Guid templateId,
        DateOnly reviewPeriodStart,
        DateOnly reviewPeriodEnd,
        DateOnly submissionDeadline,
        Guid? goalCycleId = null,
        Guid? okrCycleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (reviewPeriodEnd <= reviewPeriodStart)
            throw new ArgumentException("ReviewPeriodEnd must be after ReviewPeriodStart.");
        if (submissionDeadline < reviewPeriodEnd)
            throw new ArgumentException("SubmissionDeadline must be on or after ReviewPeriodEnd.");

        return new ReviewCycle(Guid.NewGuid(), tenantId, name.Trim(), type, templateId,
            reviewPeriodStart, reviewPeriodEnd, submissionDeadline, goalCycleId, okrCycleId);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void StartEnrollment()
    {
        if (Status != ReviewCycleStatus.Draft)
            throw new InvalidOperationException($"Cannot start enrollment from {Status}.");
        Status = ReviewCycleStatus.Enrolling;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void BeginReviews(int totalAssignments)
    {
        if (Status != ReviewCycleStatus.Enrolling)
            throw new InvalidOperationException($"Cannot begin reviews from {Status}.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalAssignments);
        TotalAssignments = totalAssignments;
        Status = ReviewCycleStatus.InProgress;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void RecordSubmissionCompleted()
    {
        if (Status != ReviewCycleStatus.InProgress)
            throw new InvalidOperationException($"Submission recording only valid in InProgress state.");
        CompletedAssignments++;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MoveToCalibration()
    {
        if (Status != ReviewCycleStatus.InProgress)
            throw new InvalidOperationException($"Cannot move to calibration from {Status}.");
        Status = ReviewCycleStatus.Calibrating;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Complete()
    {
        if (Status != ReviewCycleStatus.Calibrating)
            throw new InvalidOperationException($"Cannot complete from {Status}.");
        Status = ReviewCycleStatus.Completed;

        RaiseDomainEvent(new ReviewCycleCompleted(
            Id, TenantId, Name, ReviewPeriodStart, ReviewPeriodEnd,
            TotalAssignments, CompletedAssignments, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Archive()
    {
        if (Status != ReviewCycleStatus.Completed)
            throw new InvalidOperationException($"Cannot archive from {Status}.");
        Status = ReviewCycleStatus.Archived;
    }
}
