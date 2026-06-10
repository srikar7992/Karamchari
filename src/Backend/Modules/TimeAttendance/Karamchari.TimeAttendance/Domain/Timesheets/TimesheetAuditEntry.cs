// -----------------------------------------------------------------------
// <copyright file="TimesheetAuditEntry.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Timesheets;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record TimesheetAuditEntry
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid AuditId { get; init; } = Guid.NewGuid();

    // "Submitted" | "Approved" | "Rejected" | "Reopened" | "EntryApproved"
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ActorId { get; init; }

    public string? Reason { get; init; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
