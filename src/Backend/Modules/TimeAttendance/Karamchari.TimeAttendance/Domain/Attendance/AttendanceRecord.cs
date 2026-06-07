using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.Attendance.Events;

namespace Karamchari.TimeAttendance.Domain.Attendance;

/// <summary>
/// Core operational attendance aggregate.
/// Created when a shift is assigned (Pending), updated as punches arrive, finalized at end of shift.
/// Payroll reads only finalized records. Regularization corrects exceptions.
/// </summary>
public sealed class AttendanceRecord : AggregateRoot<Guid>, ITenantOwned
{
    private AttendanceRecord() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }

    // Roster linkage — who was assigned to work when
    public Guid ShiftId { get; private set; }
    public Guid ShiftAssignmentId { get; private set; }
    public Guid? SessionId { get; private set; }

    public DateOnly WorkDate { get; private set; }
    public DateTimeOffset ExpectedStart { get; private set; }
    public DateTimeOffset ExpectedEnd { get; private set; }

    public DateTimeOffset? ActualStart { get; private set; }
    public DateTimeOffset? ActualEnd { get; private set; }

    public AttendanceStatus Status { get; private set; }

    public int TotalWorkedMinutes { get; private set; }
    public int OvertimeMinutes { get; private set; }
    public int AutoBreakDeductedMinutes { get; private set; }

    // Set when the shift's NightDifferentialMultiplier > 1.0 and IsNightShift == true
    public bool NightDifferentialApplied { get; private set; }
    public decimal NightDifferentialMultiplier { get; private set; } = 1.0m;

    // Kept for backward compatibility with ReconciliationService
    public decimal WorkedHours => TotalWorkedMinutes / 60m;
    public decimal OvertimeHours => OvertimeMinutes / 60m;

    public bool IsRegularized { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? FinalizedAtUtc { get; private set; }

    // Optimistic concurrency — prevents double check-in race and checkout vs finalization race.
    // EF will throw DbUpdateConcurrencyException if two writers conflict on the same row.
    public byte[] RowVersion { get; private set; } = [];

    // Legacy field — kept for existing index/queries
    public DateOnly Date => WorkDate;

    /// <summary>
    /// Phase 3 factory: creates a Pending record when a shift is published and assigned.
    /// This is the starting point for attendance intelligence.
    /// </summary>
    public static AttendanceRecord CreatePending(
        string tenantId,
        Guid employeeId,
        Guid shiftId,
        Guid shiftAssignmentId,
        DateOnly workDate,
        DateTimeOffset expectedStart,
        DateTimeOffset expectedEnd)
    {
        if (expectedEnd <= expectedStart)
            throw new ArgumentException("ExpectedEnd must be after ExpectedStart.");

        var record = new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            ShiftId = shiftId,
            ShiftAssignmentId = shiftAssignmentId,
            WorkDate = workDate,
            ExpectedStart = expectedStart,
            ExpectedEnd = expectedEnd,
            Status = AttendanceStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        record.RaiseDomainEvent(new AttendanceRecordCreated(
            record.Id, tenantId, employeeId, shiftId, workDate, expectedStart, expectedEnd));

        return record;
    }

    /// <summary>
    /// Records actual check-in time. Determines if employee is on time or late.
    /// Grace window is evaluated externally by AttendanceProcessingEngine using policy.
    /// </summary>
    public void RecordCheckIn(Guid sessionId, DateTimeOffset actualStart)
    {
        if (Status != AttendanceStatus.Pending && Status != AttendanceStatus.Absent)
            throw new InvalidOperationException($"Cannot record check-in from status {Status}.");

        SessionId = sessionId;
        ActualStart = actualStart;
        Status = AttendanceStatus.CheckedIn;
    }

    /// <summary>
    /// Records actual check-out and computes TotalWorkedMinutes, determines final status.
    /// Called by AttendanceProcessingEngine after policy evaluation.
    /// </summary>
    public void RecordCheckOut(
        DateTimeOffset actualEnd,
        int totalWorkedMinutes,
        int overtimeMinutes,
        AttendanceStatus finalStatus,
        int autoBreakDeductedMinutes = 0,
        bool nightDifferentialApplied = false,
        decimal nightDifferentialMultiplier = 1.0m)
    {
        if (Status != AttendanceStatus.CheckedIn && Status != AttendanceStatus.ReturnedFromBreak)
            throw new InvalidOperationException($"Cannot record check-out from status {Status}.");

        if (actualEnd <= ActualStart)
            throw new ArgumentException("Check-out must be after check-in.");

        ActualEnd = actualEnd;
        TotalWorkedMinutes = totalWorkedMinutes;
        OvertimeMinutes = overtimeMinutes;
        AutoBreakDeductedMinutes = autoBreakDeductedMinutes;
        NightDifferentialApplied = nightDifferentialApplied;
        NightDifferentialMultiplier = nightDifferentialMultiplier;
        Status = finalStatus;
        FinalizedAtUtc = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new AttendanceStatusDetermined(Id, TenantId, EmployeeId, finalStatus, totalWorkedMinutes));
        RaiseDomainEvent(new WorkedMinutesCalculated(Id, TenantId, EmployeeId, WorkDate, totalWorkedMinutes, overtimeMinutes));

        if (overtimeMinutes > 0)
            RaiseDomainEvent(new OvertimeDetected(Id, TenantId, EmployeeId, WorkDate, overtimeMinutes));
    }

    /// <summary>
    /// Marks employee as absent (no show). Called by end-of-day batch job.
    /// </summary>
    public void MarkAbsent()
    {
        if (Status != AttendanceStatus.Pending)
            throw new InvalidOperationException($"Cannot mark absent from status {Status}. Only Pending records can be marked Absent by the system.");

        Status = AttendanceStatus.Absent;
        TotalWorkedMinutes = 0;
        FinalizedAtUtc = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new AttendanceStatusDetermined(Id, TenantId, EmployeeId, AttendanceStatus.Absent, 0));
    }

    /// <summary>
    /// Marks employee as incomplete (checked in, shift ended, no check-out recorded).
    /// </summary>
    public void MarkIncomplete(int workedMinutesEstimate)
    {
        if (Status != AttendanceStatus.CheckedIn && Status != AttendanceStatus.ReturnedFromBreak)
            throw new InvalidOperationException($"Cannot mark incomplete from status {Status}.");

        Status = AttendanceStatus.Incomplete;
        TotalWorkedMinutes = workedMinutesEstimate;
        FinalizedAtUtc = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new AttendanceStatusDetermined(Id, TenantId, EmployeeId, AttendanceStatus.Incomplete, workedMinutesEstimate));
    }

    /// <summary>
    /// Marks employee as on approved leave. Called when leave system signals overlap.
    /// </summary>
    public void MarkOnLeave()
    {
        if (Status != AttendanceStatus.Pending && Status != AttendanceStatus.Absent)
            throw new InvalidOperationException($"Cannot mark OnLeave from status {Status}.");

        Status = AttendanceStatus.OnLeave;
        FinalizedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Applies a regularization approval. Updates status to Present and marks regularized.
    /// Only called after RegularizationRequest.Approve() raises RegularizationApproved event.
    /// </summary>
    public void ApplyRegularization()
    {
        if (Status == AttendanceStatus.Present)
            throw new InvalidOperationException("Record already Present. Regularization not needed.");

        IsRegularized = true;
        Status = AttendanceStatus.Present;
        FinalizedAtUtc = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new AttendanceFinalized(Id, TenantId, EmployeeId, WorkDate, AttendanceStatus.Present, TotalWorkedMinutes, true));
    }

    /// <summary>
    /// Finalize and publish to payroll integration. Called when payroll period closes.
    /// </summary>
    public void FinalizeForPayroll()
    {
        if (Status is AttendanceStatus.Pending or AttendanceStatus.CheckedIn)
            throw new InvalidOperationException($"Cannot finalize record in state {Status}.");

        FinalizedAtUtc ??= DateTimeOffset.UtcNow;

        RaiseDomainEvent(new AttendanceFinalized(Id, TenantId, EmployeeId, WorkDate, Status, TotalWorkedMinutes, IsRegularized));
    }

    // --- Backward compatibility for AttendanceReconciliationService ---

    public static AttendanceRecord FinalizeLegacy(
        string tenantId,
        Guid employeeId,
        DateOnly date,
        Guid? shiftId,
        AttendanceStatus status,
        decimal workedHours,
        decimal overtimeHours,
        bool isRegularized = false)
    {
        return new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            WorkDate = date,
            ShiftId = shiftId ?? Guid.Empty,
            Status = status,
            TotalWorkedMinutes = (int)(workedHours * 60),
            OvertimeMinutes = (int)(overtimeHours * 60),
            IsRegularized = isRegularized,
            CreatedAt = DateTimeOffset.UtcNow,
            FinalizedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}
