namespace Karamchari.TimeAttendance.Services.Validation.Rules;

using Karamchari.TimeAttendance.Domain.Leaves;

/// <summary>
/// Detects sandwich leave pattern: employee takes leave on Fri + Mon with Sat/Sun in between.
/// When AllowHolidaySandwich = false, such patterns are blocked.
///
/// Algorithm:
///   Walk backward from StartDate through non-working days.
///   If we exit the non-working block and find an existing leave day → sandwich before.
///   Walk forward from EndDate through non-working days.
///   If we exit and find an existing leave day → sandwich after.
/// </summary>
public sealed class SandwichRule : ILeaveValidationRule
{
    public string RuleName => "SandwichCheck";

    public Task<LeaveValidationResult> ValidateAsync(LeaveValidationContext context, CancellationToken ct = default)
    {
        if (context.PolicyRules.AllowHolidaySandwich)
            return Task.FromResult(LeaveValidationResult.Pass());

        var holidaySet = new HashSet<DateOnly>(context.HolidayDates);
        HashSet<DateOnly> existingLeaveDates = BuildExistingLeaveDateSet(context.EmployeeActiveRequests, context.Request.Id);

        if (HasSandwichBefore(context.Request.StartDate, existingLeaveDates, holidaySet) ||
            HasSandwichAfter(context.Request.EndDate, existingLeaveDates, holidaySet))
        {
            return Task.FromResult(LeaveValidationResult.Fail(
                RuleName,
                "Leave creates a sandwich pattern over a non-working day. Policy does not permit sandwich leave."));
        }

        return Task.FromResult(LeaveValidationResult.Pass());
    }

    private static bool HasSandwichBefore(DateOnly startDate, HashSet<DateOnly> existingLeaveDates, HashSet<DateOnly> holidaySet)
    {
        DateOnly check = startDate.AddDays(-1);

        // Walk backward through non-working days
        while (IsNonWorkingDay(check, holidaySet))
        {
            check = check.AddDays(-1);
        }

        // If we passed at least one non-working day AND found a leave day on the other side → sandwich
        return check < startDate.AddDays(-1) && existingLeaveDates.Contains(check);
    }

    private static bool HasSandwichAfter(DateOnly endDate, HashSet<DateOnly> existingLeaveDates, HashSet<DateOnly> holidaySet)
    {
        DateOnly check = endDate.AddDays(1);

        while (IsNonWorkingDay(check, holidaySet))
        {
            check = check.AddDays(1);
        }

        return check > endDate.AddDays(1) && existingLeaveDates.Contains(check);
    }

    private static HashSet<DateOnly> BuildExistingLeaveDateSet(IReadOnlyList<LeaveRequest> requests, Guid excludeId)
    {
        var dates = new HashSet<DateOnly>();
        foreach (LeaveRequest r in requests)
        {
            if (r.Id == excludeId) continue;
            if (r.Status is LeaveRequestStatus.Rejected or LeaveRequestStatus.Cancelled
                or LeaveRequestStatus.Withdrawn or LeaveRequestStatus.Expired) continue;

            DateOnly d = r.StartDate;
            while (d <= r.EndDate)
            {
                dates.Add(d);
                d = d.AddDays(1);
            }
        }
        return dates;
    }

    internal static bool IsNonWorkingDay(DateOnly date, HashSet<DateOnly> holidaySet)
        => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || holidaySet.Contains(date);
}
