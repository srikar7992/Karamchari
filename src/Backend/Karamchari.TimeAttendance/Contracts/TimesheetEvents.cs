namespace Karamchari.TimeAttendance.Contracts;

/// <summary>
/// Event published when a weekly timesheet is approved.
/// This acts as a bridge to the Payroll context to record billable hours.
/// </summary>
/// <param name="TimesheetId">The timesheet identifier.</param>
/// <param name="EmployeeId">The employee identifier.</param>
/// <param name="WeekStartDate">The start date of the week.</param>
/// <param name="TotalHours">The total approved hours.</param>
public record TimesheetApprovedIntegrationEvent(
    Guid TimesheetId,
    Guid EmployeeId,
    DateOnly WeekStartDate,
    decimal TotalHours);
