namespace Karamchari.TimeAttendance.Domain.Timesheets;

/// <summary>
/// Represents a single day's time entry within a weekly timesheet.
/// </summary>
public sealed record TimeEntry
{
    /// <summary>
    /// Gets the date for this entry.
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// Gets the number of billable hours worked on this date.
    /// </summary>
    public decimal Hours { get; init; }

    /// <summary>
    /// Gets a brief description of the work performed.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Validates the time entry.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if hours are negative or exceed 24.</exception>
    public void Validate()
    {
        if (Hours < 0 || Hours > 24)
        {
            throw new ArgumentOutOfRangeException(nameof(Hours), "Hours must be between 0 and 24.");
        }
    }
}
