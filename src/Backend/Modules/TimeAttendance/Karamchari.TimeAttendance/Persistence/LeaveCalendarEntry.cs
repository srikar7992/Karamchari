// -----------------------------------------------------------------------
// <copyright file="LeaveCalendarEntry.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Persistence;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Materialized read model: one row per employee per leave day.
/// Written by LeaveRequestApproved consumer. Used for:
///   - Overlap detection (O(1) lookup vs scanning LeaveRequests)
///   - Team heatmaps
///   - Roster integration
///   - Capacity planning
/// </summary>
public sealed class LeaveCalendarEntry : ITenantOwned
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public DateOnly Date { get; init; }
    public Guid LeaveRequestId { get; init; }
    public Guid LeaveTypeId { get; init; }

    /// <summary>0.5 for half-day, fractional for hourly, 1.0 for full day.</summary>
    public decimal Duration { get; init; }

    public LeaveCalendarEntryStatus Status { get; set; }
    public DateTimeOffset LastUpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public enum LeaveCalendarEntryStatus
{
    Pending = 1,
    Approved = 2,
    Cancelled = 3,
    Rejected = 4
}
