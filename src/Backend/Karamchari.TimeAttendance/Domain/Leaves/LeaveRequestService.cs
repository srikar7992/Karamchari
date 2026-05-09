namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.TimeAttendance.Domain.Holidays;

/// <summary>
/// Domain service for processing leave requests and calculating actual leave days.
/// </summary>
public sealed class LeaveRequestService
{
    /// <summary>
    /// Calculates the number of leave days between two dates, excluding weekends and holidays.
    /// </summary>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    /// <param name="holidays">The list of holiday dates.</param>
    /// <param name="allowHalfDays">Whether half-days are allowed (reserved for future use).</param>
    /// <returns>The total number of leave days.</returns>
    public static double CalculateActualLeaveDays(
        DateOnly start,
        DateOnly end,
        IEnumerable<DateOnly> holidays,
        bool allowHalfDays)
    {
        double totalDays = 0;
        var current = start;

        while (current <= end)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday &&
                current.DayOfWeek != DayOfWeek.Sunday &&
                !holidays.Contains(current))
            {
                totalDays += 1.0;
            }
            current = current.AddDays(1);
        }

        return totalDays;
    }
}
