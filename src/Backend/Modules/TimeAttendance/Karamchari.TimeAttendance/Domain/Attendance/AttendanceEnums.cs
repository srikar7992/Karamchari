// -----------------------------------------------------------------------
// <copyright file="AttendanceEnums.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Attendance;

public enum AttendanceStatus
{
    // Session-level (raw punch states)
    Scheduled,
    CheckedIn,
    OnBreak,
    ReturnedFromBreak,
    CheckedOut,
    Missed,
    Corrected,
    Disputed,

    // Record-level (finalized states)
    Pending,      // Shift assigned, awaiting attendance
    Present,      // On time and completed shift
    Late,         // Arrived after tolerance window
    Absent,       // No show
    HalfDay,      // Worked >= half-day threshold but < full-day
    Incomplete,   // Checked in, never checked out (end of day)
    OnLeave,      // Approved leave exists for this date
    Holiday,      // Public holiday
    WeekOff       // Scheduled day off
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum AttendanceSource
{
    WebPortal,
    MobileApp,
    Kiosk,
    BiometricDevice,
    BulkImport,
    SystemAutoAction
}
