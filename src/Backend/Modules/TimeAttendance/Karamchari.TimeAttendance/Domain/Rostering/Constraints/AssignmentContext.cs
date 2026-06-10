// -----------------------------------------------------------------------
// <copyright file="AssignmentContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Rostering.Constraints;

/// <summary>
/// All external data needed for hard-constraint evaluation of a single assignment attempt.
/// Loaded by the API layer before calling Roster.Assign(), keeping domain pure.
/// </summary>
public sealed record AssignmentContext
{
    /// <summary>Approved leave date ranges for the employee (inclusive bounds).</summary>
    public IReadOnlyList<LeaveWindow> ApprovedLeaves { get; init; } = [];

    /// <summary>
    /// Employee declared availability windows per day of week.
    /// Empty = no preference declared — availability constraint skipped.
    /// </summary>
    public IReadOnlyList<AvailabilityWindow> AvailabilityWindows { get; init; } = [];

    /// <summary>All active shift assignments this calendar week (for rest period + overtime calculation).</summary>
    public IReadOnlyList<AssignedShiftWindow> WeeklyAssignments { get; init; } = [];

    /// <summary>Total hours worked this week across all existing assignments (pre-computed for performance).</summary>
    public decimal WeeklyHoursWorked { get; init; }

    /// <summary>Employee's currently active skills.</summary>
    public IReadOnlyList<EmployeeSkill> Skills { get; init; } = [];

    /// <summary>
    /// Overtime measurement policy for this employee.
    /// Determines both the hour cap and the window definition (calendar week, payroll week, etc.).
    /// </summary>
    public OvertimePolicy OvertimePolicy { get; init; } = OvertimePolicy.Default;

    /// <summary>
    /// Assignments over a wider fairness lookback window (default 4 weeks) for night-shift and
    /// weekend fairness scoring. Deliberately wider than <see cref="WeeklyAssignments"/> (1 week,
    /// used for overtime) — fairness complaints emerge from patterns over months, not days.
    /// </summary>
    public IReadOnlyList<AssignedShiftWindow> FairnessHistoryAssignments { get; init; } = [];
}

public sealed record LeaveWindow(DateOnly Start, DateOnly End);

public sealed record AvailabilityWindow(DayOfWeek DayOfWeek, TimeOnly From, TimeOnly To);

public sealed record AssignedShiftWindow(DateOnly WorkDate, TimeOnly StartTime, TimeOnly EndTime, ShiftType ShiftType);
