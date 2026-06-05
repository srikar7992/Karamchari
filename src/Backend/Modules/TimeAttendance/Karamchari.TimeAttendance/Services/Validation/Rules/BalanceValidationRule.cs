namespace Karamchari.TimeAttendance.Services.Validation.Rules;

public sealed class BalanceValidationRule : ILeaveValidationRule
{
    public string RuleName => "BalanceCheck";

    public Task<LeaveValidationResult> ValidateAsync(LeaveValidationContext context, CancellationToken ct = default)
    {
        if (context.PolicyRules.AllowNegativeBalance)
            return Task.FromResult(LeaveValidationResult.Pass());

        var required = (decimal)context.Request.ActualDays;
        if (context.CurrentBalance < required)
        {
            return Task.FromResult(LeaveValidationResult.Fail(
                RuleName,
                $"Insufficient balance. Available: {context.CurrentBalance:F2} days, required: {required:F2} days."));
        }

        return Task.FromResult(LeaveValidationResult.Pass());
    }
}
