namespace Karamchari.TimeAttendance.Services.Validation.Rules;

public sealed class NoticePeriodRule : ILeaveValidationRule
{
    public string RuleName => "NoticePeriod";

    public Task<LeaveValidationResult> ValidateAsync(LeaveValidationContext context, CancellationToken ct = default)
    {
        int requiredDays = context.PolicyRules.NoticeRequiredDays;
        if (requiredDays <= 0)
            return Task.FromResult(LeaveValidationResult.Pass());

        int daysUntilLeave = context.Request.StartDate.DayNumber - context.TodayInEmployeeZone.DayNumber;
        if (daysUntilLeave < requiredDays)
        {
            return Task.FromResult(LeaveValidationResult.Fail(
                RuleName,
                $"Minimum {requiredDays} day(s) notice required. Leave starts in {daysUntilLeave} day(s)."));
        }

        return Task.FromResult(LeaveValidationResult.Pass());
    }
}
