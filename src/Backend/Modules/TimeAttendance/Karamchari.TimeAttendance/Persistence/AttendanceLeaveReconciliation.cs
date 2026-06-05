namespace Karamchari.TimeAttendance.Persistence;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Read model: compares approved leave vs actual attendance per employee per day.
/// Drives payroll disputes prevention and anomaly detection.
///
/// Scenarios this catches:
///   Conflict_LeaveAbsent  → leave approved but employee did NOT attend (as expected)
///   Conflict_LeavePresent → leave approved but attendance shows PRESENT (employee worked on leave day)
///   Conflict_AbsentNoLeave → absent with no approved leave (unauthorized absence / pending LOP)
/// </summary>
public sealed class AttendanceLeaveReconciliation : ITenantOwned
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public DateOnly Date { get; init; }
    public ReconciliationStatus Status { get; set; }
    public Guid? LeaveRequestId { get; init; }
    public Guid? AttendanceRecordId { get; init; }

    /// <summary>Days expected from leave approval (0.5 for half-day, etc.).</summary>
    public decimal ExpectedLeaveDays { get; set; }

    /// <summary>Actual hours logged by badge/biometric system.</summary>
    public decimal ActualAttendanceHours { get; set; }

    public string? ResolutionNotes { get; set; }
    public bool IsResolvedByHR { get; set; }
    public DateTimeOffset LastUpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public enum ReconciliationStatus
{
    /// <summary>Leave approved + employee absent as expected.</summary>
    Matched = 1,

    /// <summary>Leave approved but attendance system shows employee was present.</summary>
    ConflictLeavePresent = 2,

    /// <summary>Employee absent with no corresponding approved leave — potential LOP.</summary>
    ConflictAbsentNoLeave = 3,

    /// <summary>Awaiting attendance data to complete reconciliation.</summary>
    Pending = 4,

    /// <summary>HR manually resolved the discrepancy.</summary>
    ManuallyResolved = 5
}
