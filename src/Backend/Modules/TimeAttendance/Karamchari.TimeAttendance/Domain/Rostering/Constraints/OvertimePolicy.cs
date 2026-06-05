namespace Karamchari.TimeAttendance.Domain.Rostering.Constraints;

/// <summary>
/// Declares the measurement window and hour cap for overtime enforcement.
/// Different organizations (and unions) define the "week" differently —
/// making the window explicit prevents silent compliance failures.
/// </summary>
public sealed record OvertimePolicy
{
    /// <summary>Calendar week, 48h maximum — default for most jurisdictions without a contract override.</summary>
    public static readonly OvertimePolicy Default = new()
    {
        MeasurementWindow = OvertimeMeasurementWindow.CalendarWeek,
        MaximumHours = 48m,
    };

    public OvertimeMeasurementWindow MeasurementWindow { get; init; } = OvertimeMeasurementWindow.CalendarWeek;
    public decimal MaximumHours { get; init; } = 48m;
}

public enum OvertimeMeasurementWindow
{
    /// <summary>
    /// Sunday–Saturday calendar week.
    /// WeeklyHoursWorked query uses Sunday as week start (DayOfWeek cast to int).
    /// </summary>
    CalendarWeek = 1,

    /// <summary>
    /// Employer-defined payroll period.
    /// Requires a payroll cycle start date from the employment contract — not yet wired.
    /// </summary>
    PayrollWeek = 2,

    /// <summary>
    /// Relative to the current roster's StartDate.
    /// Useful for projects where the schedule week doesn't align with the calendar.
    /// </summary>
    RosterWeek = 3,
}
