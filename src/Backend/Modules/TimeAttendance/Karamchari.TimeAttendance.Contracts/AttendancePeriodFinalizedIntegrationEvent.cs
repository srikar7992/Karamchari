namespace Karamchari.TimeAttendance.Contracts;

/// <summary>
/// Published at period close when attendance finalization completes.
/// Consumed by Payroll module to trigger salary computation for the period.
/// Contains per-employee summary sufficient for payroll without re-querying attendance.
/// </summary>
public sealed record AttendancePeriodFinalizedIntegrationEvent(
    Guid CorrelationId,
    string TenantId,
    int Year,
    int Month,
    DateTimeOffset FinalizedAtUtc,
    IReadOnlyList<AttendanceEmployeeSummary> Summaries);

public sealed record AttendanceEmployeeSummary(
    Guid EmployeeId,
    int PresentDays,
    int AbsentDays,
    int LateDays,
    int HalfDays,
    int LeaveDays,
    int HolidayWorkDays,
    decimal TotalWorkedHours,
    decimal OvertimeHours,
    decimal CompOffDaysAccrued,
    decimal LopDays);

/// <summary>
/// Published when a single attendance record is finalized (real-time mode for near-real-time payroll systems).
/// </summary>
public sealed record AttendanceRecordFinalizedIntegrationEvent(
    Guid CorrelationId,
    string TenantId,
    Guid EmployeeId,
    DateOnly WorkDate,
    string Status,
    decimal WorkedHours,
    decimal OvertimeHours,
    bool IsRegularized,
    DateTimeOffset FinalizedAtUtc);

/// <summary>
/// Published when a biometric enrollment completes. Consumed by access control module to sync door access.
/// </summary>
public sealed record BiometricEnrollmentCompletedIntegrationEvent(
    Guid CorrelationId,
    string TenantId,
    Guid EmployeeId,
    Guid TemplateId,
    string Modality,
    IReadOnlyList<Guid> AuthorizedDeviceIds,
    DateTimeOffset EnrolledAtUtc);

/// <summary>
/// Published when a geo fraud signal of High severity is detected.
/// Consumed by the Notifications module and HR dashboard.
/// </summary>
public sealed record GeoFraudDetectedIntegrationEvent(
    Guid CorrelationId,
    string TenantId,
    Guid EmployeeId,
    Guid? PunchEventId,
    string FraudType,
    string Severity,
    string Description,
    DateTimeOffset DetectedAtUtc);

/// <summary>
/// Published when consecutive absence escalation threshold is breached.
/// Consumed by Notifications and HR modules.
/// </summary>
public sealed record ConsecutiveAbsenceEscalatedIntegrationEvent(
    Guid CorrelationId,
    string TenantId,
    Guid EmployeeId,
    int ConsecutiveDays,
    DateOnly FirstAbsentDate,
    DateOnly LastAbsentDate,
    DateTimeOffset DetectedAtUtc);

/// <summary>
/// Published when an attendance violation is raised (late arrival, no-show, missing check-out, etc.).
/// Consumed by Notifications module to alert employee and manager.
/// </summary>
public sealed record AttendanceExceptionRaisedIntegrationEvent(
    Guid CorrelationId,
    string TenantId,
    Guid EmployeeId,
    Guid AttendanceRecordId,
    string ExceptionType,
    string Severity,
    string Description,
    DateTimeOffset RaisedAtUtc);

/// <summary>
/// Published when a regularization request is approved.
/// Consumed by Notifications module to confirm outcome to employee.
/// </summary>
public sealed record RegularizationApprovedIntegrationEvent(
    Guid CorrelationId,
    string TenantId,
    Guid EmployeeId,
    Guid RegularizationRequestId,
    Guid ApprovedByEmployeeId,
    DateTimeOffset ApprovedAtUtc);

/// <summary>
/// Published when a regularization request is rejected.
/// Consumed by Notifications module to inform employee of outcome.
/// </summary>
public sealed record RegularizationRejectedIntegrationEvent(
    Guid CorrelationId,
    string TenantId,
    Guid EmployeeId,
    Guid RegularizationRequestId,
    string Reason,
    DateTimeOffset RejectedAtUtc);
