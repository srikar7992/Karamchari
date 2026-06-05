namespace Karamchari.TimeAttendance.Services.Validation;

using Karamchari.TimeAttendance.Domain.Leaves;

/// <summary>
/// Single validation rule in the leave application pipeline.
/// Rules run in order via LeaveValidationEngine. Each rule is independent.
/// </summary>
public interface ILeaveValidationRule
{
    string RuleName { get; }

    Task<LeaveValidationResult> ValidateAsync(
        LeaveValidationContext context,
        CancellationToken ct = default);
}

/// <summary>
/// Context passed to every validation rule. Built once per application attempt.
/// </summary>
public sealed class LeaveValidationContext
{
    public required LeaveRequest Request { get; init; }
    public required LeavePolicyRules PolicyRules { get; init; }
    public required decimal CurrentBalance { get; init; }
    public required IReadOnlyList<DateOnly> HolidayDates { get; init; }
    public required IReadOnlyList<Persistence.LeaveCalendarEntry> TeamCalendar { get; init; }
    public required IReadOnlyList<LeaveRequest> EmployeeActiveRequests { get; init; }
    public required string EmployeeTimeZoneId { get; init; }
    public required DateOnly TodayInEmployeeZone { get; init; }
    public required IReadOnlyList<Persistence.LeaveBlackoutPeriod> BlackoutPeriods { get; init; }
}

/// <summary>
/// Result from a single validation rule.
/// </summary>
public sealed class LeaveValidationResult
{
    public static LeaveValidationResult Pass() => new() { IsValid = true };

    public static LeaveValidationResult Fail(string ruleName, string message,
        ValidationSeverity severity = ValidationSeverity.Error)
        => new()
        {
            IsValid = false,
            RuleName = ruleName,
            Message = message,
            Severity = severity
        };

    public bool IsValid { get; init; }
    public string? RuleName { get; init; }
    public string? Message { get; init; }
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
}

public enum ValidationSeverity
{
    Warning = 1,
    Error = 2
}
