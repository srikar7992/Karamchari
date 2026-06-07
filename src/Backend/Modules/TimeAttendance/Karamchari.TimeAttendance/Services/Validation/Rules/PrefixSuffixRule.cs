namespace Karamchari.TimeAttendance.Services.Validation.Rules;

/// <summary>
/// Blocks leave that directly prefixes or suffixes a public holiday or weekend.
/// When AllowPrefixSuffix = false:
///   - Leave ending the day before a holiday/weekend is blocked (prefix extension)
///   - Leave starting the day after a holiday/weekend is blocked (suffix extension)
///
/// Example: Thursday public holiday → Wednesday leave blocked (suffix into Thursday),
///          Friday leave blocked (prefix after Thursday).
/// </summary>
public sealed class PrefixSuffixRule : ILeaveValidationRule
{
    public string RuleName => "PrefixSuffixCheck";

    public Task<LeaveValidationResult> ValidateAsync(LeaveValidationContext context, CancellationToken ct = default)
    {
        if (context.PolicyRules.AllowPrefixSuffix)
            return Task.FromResult(LeaveValidationResult.Pass());

        var holidaySet = new HashSet<DateOnly>(context.HolidayDates);

        DateOnly dayAfterEnd = context.Request.EndDate.AddDays(1);
        DateOnly dayBeforeStart = context.Request.StartDate.AddDays(-1);

        bool endsBeforeNonWorking = IsNonWorkingDay(dayAfterEnd, holidaySet);
        bool startsAfterNonWorking = IsNonWorkingDay(dayBeforeStart, holidaySet);

        if (endsBeforeNonWorking)
        {
            return Task.FromResult(LeaveValidationResult.Fail(
                RuleName,
                $"Leave ends on {context.Request.EndDate:d} which is immediately before a non-working day ({dayAfterEnd:d}). Prefix/suffix leave is not permitted under this policy."));
        }

        if (startsAfterNonWorking)
        {
            return Task.FromResult(LeaveValidationResult.Fail(
                RuleName,
                $"Leave starts on {context.Request.StartDate:d} which is immediately after a non-working day ({dayBeforeStart:d}). Prefix/suffix leave is not permitted under this policy."));
        }

        return Task.FromResult(LeaveValidationResult.Pass());
    }

    private static bool IsNonWorkingDay(DateOnly date, HashSet<DateOnly> holidaySet)
        => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || holidaySet.Contains(date);
}
