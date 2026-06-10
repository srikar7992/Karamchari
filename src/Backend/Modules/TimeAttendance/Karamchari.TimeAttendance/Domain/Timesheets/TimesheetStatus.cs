// -----------------------------------------------------------------------
// <copyright file="TimesheetStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Timesheets;

/// <summary>
/// Defines the lifecycle states of a weekly timesheet.
/// </summary>
public enum TimesheetStatus
{
    /// <summary>The employee is still editing the timesheet.</summary>
    Draft = 0,

    /// <summary>The timesheet has been submitted for manager approval.</summary>
    Submitted = 1,

    /// <summary>The timesheet has been approved and is ready for payroll processing.</summary>
    Approved = 2,

    /// <summary>The timesheet was rejected and requires corrections.</summary>
    Rejected = 3
}
