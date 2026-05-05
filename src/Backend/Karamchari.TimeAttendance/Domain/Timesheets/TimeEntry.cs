namespace Karamchari.TimeAttendance.Domain.Timesheets;

/// <summary>
/// Represents a single day's time entry within a weekly timesheet.
/// A timesheet row can now carry project-level metadata for PSA billing.
/// </summary>
public sealed record TimeEntry
{
    /// <summary>
    /// Gets the date for this entry.
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// Gets the number of hours worked on this date.
    /// </summary>
    public decimal Hours { get; init; }

    /// <summary>
    /// Gets a brief description of the work performed.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the project this time is logged against.
    /// Null indicates internal/overhead time with no associated client project.
    /// </summary>
    public Guid? ProjectId { get; init; }

    /// <summary>
    /// Gets whether these hours are billable to the client.
    /// For NonBillable projects this must always be false (enforced in <see cref="Validate"/>).
    /// </summary>
    public bool IsBillable { get; init; }

    /// <summary>
    /// Validates the time entry.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if hours are negative or exceed 24.</exception>
    /// <exception cref="InvalidOperationException">Thrown if IsBillable is true but no ProjectId is set.</exception>
    public void Validate()
    {
        if (Hours < 0 || Hours > 24)
        {
            throw new ArgumentOutOfRangeException(nameof(Hours), "Hours must be between 0 and 24.");
        }

        if (IsBillable && ProjectId == null)
        {
            throw new InvalidOperationException(
                "A time entry cannot be marked billable without an associated ProjectId.");
        }
    }
}
