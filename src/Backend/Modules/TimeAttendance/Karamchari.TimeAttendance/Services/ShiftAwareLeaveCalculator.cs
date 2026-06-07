namespace Karamchari.TimeAttendance.Services;

using Karamchari.TimeAttendance.Domain.Leaves;

/// <summary>
/// Shift-aware leave day calculator. Replaces standard Mon-Fri logic for blue-collar scenarios.
///
/// Handles:
///   - Custom work patterns (Tue-Sat, Sun-Thu, etc.)
///   - Rotational rosters (4-on-2-off)
///   - Night shifts that span calendar day boundaries
///
/// All existing IT-centric code uses LeaveRequestService.CalculateActualLeaveDays.
/// Blue-collar flows use this service instead.
/// </summary>
public sealed class ShiftAwareLeaveCalculator
{
    /// <summary>
    /// Calculates working days between two dates respecting employee's work pattern.
    /// Non-working days in the pattern are excluded (like weekends are for IT employees).
    /// </summary>
    public static double CalculateLeaveDays(
        DateOnly start,
        DateOnly end,
        WorkPattern workPattern,
        IEnumerable<DateOnly> holidays)
    {
        ISet<DateOnly> holidaySet = holidays as ISet<DateOnly> ?? new HashSet<DateOnly>(holidays);
        double totalDays = 0;
        DateOnly current = start;

        while (current <= end)
        {
            if (workPattern.IsWorkingDay(current) && !holidaySet.Contains(current))
                totalDays += 1.0;

            current = current.AddDays(1);
        }

        return totalDays;
    }

    /// <summary>
    /// For night shifts that span calendar boundaries (e.g. 22:00-06:00):
    /// determines which calendar date the leave should be attributed to.
    ///
    /// Convention: leave is attributed to the date the shift STARTS, not ends.
    /// A shift starting at 22:00 on June 5 and ending 06:00 on June 6 is "June 5 leave."
    /// </summary>
    public static DateOnly GetEffectiveLeaveDate(DateTime shiftStart, DateTime shiftEnd, DateTime leaveStartTime)
    {
        if (leaveStartTime < shiftStart || leaveStartTime >= shiftEnd)
            throw new ArgumentOutOfRangeException(nameof(leaveStartTime),
                "Leave start time must fall within the shift window.");

        return DateOnly.FromDateTime(shiftStart);
    }

    /// <summary>
    /// Determines how many partial shift hours constitute a leave deduction.
    /// For a 4-hour block on an 8-hour shift → 0.5 days leave.
    /// </summary>
    public static double CalculateShiftFraction(double shiftHours, double leaveHours)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(shiftHours);
        if (leaveHours < 0 || leaveHours > shiftHours)
            throw new ArgumentOutOfRangeException(nameof(leaveHours));

        return leaveHours / shiftHours;
    }

    /// <summary>
    /// Checks whether a date is a week-off for the employee (non-working day in their pattern).
    /// Used to prevent leave deduction on scheduled off days.
    /// </summary>
    public static bool IsWeekOff(DateOnly date, WorkPattern workPattern)
        => !workPattern.IsWorkingDay(date);
}
