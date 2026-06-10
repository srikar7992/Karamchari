// -----------------------------------------------------------------------
// <copyright file="ReviewTaskInboxItem.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Reviews;

namespace Karamchari.Performance.Domain.ReadModels;

/// <summary>
/// One row per pending review assignment for a reviewer.
/// Drives the "My Review Inbox" experience â€” all pending tasks in one query.
/// </summary>
public sealed class ReviewTaskInboxItem : Entity<Guid>, ITenantOwned
{
    private ReviewTaskInboxItem() { /* EF materialization */ }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ReviewerEmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ReviewAssignmentId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ReviewCycleId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CycleName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RevieweeEmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string RevieweeDisplayName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ReviewerRole ReviewerRole { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset Deadline { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsOverdue { get; private set; }

    /// <summary>Hours until deadline. Negative when overdue.</summary>
    public int HoursUntilDeadline { get; private set; }

    /// <summary>Priority score used to sort the inbox: overdue + self-review + direct report outrank peers.</summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset LastRefreshedUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static ReviewTaskInboxItem Create(
        string tenantId,
        Guid reviewerEmployeeId,
        Guid reviewAssignmentId,
        Guid reviewCycleId,
        string cycleName,
        Guid revieweeEmployeeId,
        string revieweeDisplayName,
        ReviewerRole reviewerRole,
        DateTimeOffset deadline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(cycleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(revieweeDisplayName);

        var now = DateTimeOffset.UtcNow;
        var hoursUntil = (int)(deadline - now).TotalHours;
        var isOverdue = hoursUntil < 0;
        var priority = ComputePriority(isOverdue, reviewerRole, hoursUntil);

        return new ReviewTaskInboxItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReviewerEmployeeId = reviewerEmployeeId,
            ReviewAssignmentId = reviewAssignmentId,
            ReviewCycleId = reviewCycleId,
            CycleName = cycleName,
            RevieweeEmployeeId = revieweeEmployeeId,
            RevieweeDisplayName = revieweeDisplayName,
            ReviewerRole = reviewerRole,
            Deadline = deadline,
            IsOverdue = isOverdue,
            HoursUntilDeadline = hoursUntil,
            Priority = priority,
            LastRefreshedUtc = now,
        };
    }

    private static int ComputePriority(bool isOverdue, ReviewerRole role, int hoursUntil)
    {
        var score = 0;
        if (isOverdue) score += 1000;
        if (role == ReviewerRole.Self) score += 200;
        if (role == ReviewerRole.Manager) score += 100;
        // Closer deadline = higher priority (max +48 for tasks due within 2 days)
        if (hoursUntil is >= 0 and <= 48) score += 48 - hoursUntil;
        return score;
    }
}
