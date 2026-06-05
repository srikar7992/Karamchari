using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.Attendance.Events;

namespace Karamchari.TimeAttendance.Domain.Attendance;

public enum RegularizationStatus
{
    Submitted,
    Approved,
    Rejected,
    Cancelled
}

/// <summary>
/// Correction workflow for attendance exceptions.
/// Domain Rules:
///   Rule 1: Only records with exceptions can be regularized (Present records rejected at creation).
///   Rule 2: Approval raises RegularizationApproved event → handler updates AttendanceRecord.
///   Rule 3: Rejection changes nothing on the attendance record.
/// </summary>
public sealed class RegularizationRequest : AggregateRoot<Guid>, ITenantOwned
{
    private RegularizationRequest() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid AttendanceRecordId { get; private set; }
    public Guid EmployeeId { get; private set; }

    public string Explanation { get; private set; } = string.Empty;

    /// <summary>Supporting evidence: file URLs, biometric device logs, manager notes, etc.</summary>
    public string? Evidence { get; private set; }

    public RegularizationStatus Status { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public string? RejectionReason { get; private set; }

    /// <summary>
    /// Creates a regularization request.
    /// attendanceStatus must NOT be Present — Rule 1.
    /// </summary>
    public static RegularizationRequest Submit(
        string tenantId,
        Guid attendanceRecordId,
        Guid employeeId,
        AttendanceStatus currentAttendanceStatus,
        string explanation,
        string? evidence = null)
    {
        // Rule 1: Present records cannot be regularized
        if (currentAttendanceStatus == AttendanceStatus.Present)
            throw new InvalidOperationException(
                "Present attendance records cannot be regularized. Only exception records (Absent, Late, Incomplete, HalfDay, MissingCheckIn, etc.) require regularization.");

        if (string.IsNullOrWhiteSpace(explanation))
            throw new ArgumentException("Explanation is required for regularization.", nameof(explanation));

        var request = new RegularizationRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AttendanceRecordId = attendanceRecordId,
            EmployeeId = employeeId,
            Explanation = explanation.Trim(),
            Evidence = evidence?.Trim(),
            Status = RegularizationStatus.Submitted,
            RequestedAt = DateTimeOffset.UtcNow,
        };

        request.RaiseDomainEvent(new RegularizationSubmitted(
            request.Id, tenantId, employeeId, attendanceRecordId));

        return request;
    }

    /// <summary>
    /// Approves the regularization.
    /// Rule 2: Raises RegularizationApproved — the event handler updates AttendanceRecord.
    /// The aggregate boundary is respected: RegularizationRequest does not touch AttendanceRecord directly.
    /// </summary>
    public void Approve(Guid approvedByUserId)
    {
        if (Status != RegularizationStatus.Submitted)
            throw new InvalidOperationException($"Cannot approve request in state {Status}.");

        Status = RegularizationStatus.Approved;
        ApprovedAt = DateTimeOffset.UtcNow;
        ApprovedBy = approvedByUserId;

        RaiseDomainEvent(new RegularizationApproved(
            Id, TenantId, EmployeeId, AttendanceRecordId, approvedByUserId));
    }

    /// <summary>
    /// Rejects the regularization.
    /// Rule 3: Attendance record is not modified.
    /// </summary>
    public void Reject(string reason)
    {
        if (Status != RegularizationStatus.Submitted)
            throw new InvalidOperationException($"Cannot reject request in state {Status}.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason required.", nameof(reason));

        Status = RegularizationStatus.Rejected;
        RejectionReason = reason.Trim();
        ApprovedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new RegularizationRejected(Id, TenantId, EmployeeId));
    }

    public void Cancel()
    {
        if (Status != RegularizationStatus.Submitted)
            throw new InvalidOperationException($"Cannot cancel request in state {Status}. Only Submitted requests can be cancelled.");

        Status = RegularizationStatus.Cancelled;
    }
}
