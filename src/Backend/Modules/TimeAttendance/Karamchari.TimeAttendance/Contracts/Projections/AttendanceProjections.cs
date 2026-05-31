namespace Karamchari.TimeAttendance.Contracts.Projections;

/// <summary>
/// Summary of attendance for a specific period, consumed by Payroll.
/// </summary>
public sealed record AttendancePayrollSummaryProjection(
    Guid EmployeeId,
    string PeriodName,
    decimal PresentDays,
    decimal AbsentDays,
    decimal LateDays,
    decimal OvertimeHours,
    decimal PayableDays);

/// <summary>
/// Detailed attendance feed for dashboards.
/// </summary>
public sealed record AttendanceDashboardProjection(
    Guid EmployeeId,
    string EmployeeName,
    DateOnly Date,
    string Status,
    DateTimeOffset? CheckIn,
    DateTimeOffset? CheckOut,
    decimal WorkedHours,
    bool IsRegularized);
