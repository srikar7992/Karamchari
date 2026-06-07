namespace Karamchari.TimeAttendance.Services.Validation.Rules;

public sealed class ConsecutiveDaysRule : ILeaveValidationRule
{
    public string RuleName => "ConsecutiveDays";

    public Task<LeaveValidationResult> ValidateAsync(LeaveValidationContext context, CancellationToken ct = default)
    {
        int? max = context.PolicyRules.MaxConsecutiveDays;
        if (max is null or <= 0)
            return Task.FromResult(LeaveValidationResult.Pass());

        if (context.Request.ActualDays > max.Value)
        {
            return Task.FromResult(LeaveValidationResult.Fail(
                RuleName,
                $"Maximum {max.Value} consecutive days allowed. Requested: {context.Request.ActualDays} days."));
        }

        return Task.FromResult(LeaveValidationResult.Pass());
    }
}
