namespace Karamchari.TimeAttendance.Contracts;

/// <summary>
/// Event emitted when a leave request is approved.
/// </summary>
/// <param name="RequestId">The leave request identifier.</param>
/// <param name="EmployeeId">The employee identifier.</param>
/// <param name="StartDate">The start date.</param>
/// <param name="EndDate">The end date.</param>
/// <param name="TotalDays">The calculated leave days (excluding holidays/weekends).</param>
/// <param name="IsPaid">Whether this leave is paid.</param>
public record LeaveRequestApprovedIntegrationEvent(
    Guid RequestId,
    Guid EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    double TotalDays,
    bool IsPaid);
