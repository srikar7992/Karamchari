namespace Karamchari.TimeAttendance.Domain.Timesheets;

/// <summary>
/// Represents a single time-log entry within a weekly timesheet.
/// Stored as an element inside the Timesheets JSON column.
/// <para>
/// Each entry carries a stable <see cref="EntryId"/> so row-level approval,
/// audit lookups, and cross-midnight splits can reference entries without relying
/// on positional index (which is fragile when entries are reordered or filtered).
/// </para>
/// </summary>
public sealed record TimeEntry
{
    /// <summary>Stable identifier — survives collection reordering and partial approvals.</summary>
    public Guid EntryId { get; init; } = Guid.NewGuid();

    /// <summary>Calendar date the work occurred (employee's local date, not UTC).</summary>
    public DateOnly Date { get; init; }

    /// <summary>Duration in decimal hours (e.g. 1.5 = 1h 30m).</summary>
    public decimal Hours { get; init; }

    /// <summary>UTC start of the work block. Required when precise overlap detection is needed.</summary>
    public DateTime? StartTimeUtc { get; init; }

    /// <summary>UTC end of the work block. Must be &gt; StartTimeUtc when provided.</summary>
    public DateTime? EndTimeUtc { get; init; }

    /// <summary>Brief description of work performed.</summary>
    public string? Description { get; init; }

    /// <summary>Project this time is logged against. Null = internal/overhead.</summary>
    public Guid? ProjectId { get; init; }

    /// <summary>Task within the project. Requires ProjectId.</summary>
    public Guid? TaskId { get; init; }

    /// <summary>Whether these hours are billable to the client. Requires ProjectId.</summary>
    public bool IsBillable { get; init; }

    /// <summary>Cost center for financial allocation (e.g. "ENG-PLATFORM").</summary>
    public string? CostCenter { get; init; }

    /// <summary>Custom tag for granular reporting (e.g. "R&amp;D", "Maintenance").</summary>
    public string? Tag { get; init; }

    /// <summary>Approval state of this individual entry (row-level approval).</summary>
    public TimeEntryStatus Status { get; init; } = TimeEntryStatus.Draft;

    /// <summary>
    /// Validates invariants on a single entry.
    /// Called before cross-entry validation in <see cref="TimesheetValidator"/>.
    /// </summary>
    public void Validate()
    {
        if (Hours <= 0 || Hours > 24)
            throw new ArgumentOutOfRangeException(nameof(Hours), $"Hours must be > 0 and ≤ 24 (got {Hours}).");

        if (StartTimeUtc.HasValue && EndTimeUtc.HasValue)
        {
            if (EndTimeUtc <= StartTimeUtc)
                throw new InvalidOperationException(
                    $"EndTimeUtc ({EndTimeUtc:O}) must be after StartTimeUtc ({StartTimeUtc:O}).");

            var duration = (decimal)(EndTimeUtc.Value - StartTimeUtc.Value).TotalHours;
            if (Math.Abs(duration - Hours) > 0.02m)
                throw new InvalidOperationException(
                    $"Hours ({Hours}) must match the UTC start/end duration ({duration:F2}h). Drift > 0.02h detected.");
        }

        if (IsBillable && ProjectId is null)
            throw new InvalidOperationException("Billable entries must have a ProjectId.");

        if (TaskId is not null && ProjectId is null)
            throw new InvalidOperationException("TaskId requires a ProjectId.");
    }
}

public enum TimeEntryStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
}
