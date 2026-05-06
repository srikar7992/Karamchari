namespace Karamchari.TimeAttendance.Domain.Timesheets;

public sealed record TimesheetAuditEntry
{
    public Guid AuditId { get; init; } = Guid.NewGuid();

    // "Submitted" | "Approved" | "Rejected" | "Reopened" | "EntryApproved"
    public string Action { get; init; } = string.Empty;

    public Guid ActorId { get; init; }

    public string? Reason { get; init; }

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
