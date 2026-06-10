// -----------------------------------------------------------------------
// <copyright file="TimesheetReopened.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.TimeAttendance.Domain.Timesheets.Events;

/// <summary>
/// Raised when an already-approved timesheet is reopened for retroactive editing.
/// Downstream systems (Payroll, Revenue) must treat any subsequent
/// <see cref="TimesheetApproved"/> for the same timesheet as a correction.
/// </summary>
public sealed record TimesheetReopened(
    Guid TimesheetId,
    Guid EmployeeId,
    string TenantId,
    DateOnly WeekStartDate,
    Guid AdminId,
    string Reason) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
