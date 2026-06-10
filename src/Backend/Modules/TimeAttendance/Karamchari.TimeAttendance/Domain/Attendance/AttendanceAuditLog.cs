// -----------------------------------------------------------------------
// <copyright file="AttendanceAuditLog.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Attendance;

/// <summary>
/// Immutable audit trail for every attendance record state change.
/// Required for labor disputes, payroll audits, regularization trails.
/// Answers: who changed what, when, why, and what was the previous value.
/// </summary>
public sealed class AttendanceAuditLog
{
    private AttendanceAuditLog() { }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid AttendanceRecordId { get; private set; }
    public Guid EmployeeId { get; private set; }

    public string Action { get; private set; } = string.Empty;
    public string? PreviousStatus { get; private set; }
    public string? NewStatus { get; private set; }
    public string ChangedBy { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    // JSON snapshot of changed fields — supports dispute resolution when status change alone is insufficient.
    // Format: {"Before":{...},"After":{...}}. Populated for regularization; null for system events.
    public string? ChangedFields { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }

    public static AttendanceAuditLog Record(
        string tenantId,
        Guid attendanceRecordId,
        Guid employeeId,
        string action,
        string? previousStatus,
        string? newStatus,
        string changedBy,
        string? reason = null,
        string? changedFields = null)
    {
        return new AttendanceAuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AttendanceRecordId = attendanceRecordId,
            EmployeeId = employeeId,
            Action = action,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedBy = changedBy,
            Reason = reason,
            ChangedFields = changedFields,
            ChangedAt = DateTimeOffset.UtcNow,
        };
    }
}
