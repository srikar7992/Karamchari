namespace Karamchari.TimeAttendance.Services.Validation.Rules;

using Karamchari.TimeAttendance.Persistence;

public sealed class BlackoutRule : ILeaveValidationRule
{
    public string RuleName => "BlackoutPeriod";

    public Task<LeaveValidationResult> ValidateAsync(LeaveValidationContext context, CancellationToken ct = default)
    {
        var start = context.Request.StartDate;
        var end = context.Request.EndDate;

        var hit = context.BlackoutPeriods
            .FirstOrDefault(b => b.StartDate <= end && b.EndDate >= start);

        if (hit is not null)
        {
            return Task.FromResult(LeaveValidationResult.Fail(
                RuleName,
                $"Leave falls within blackout period '{hit.Name}' ({hit.StartDate:d} – {hit.EndDate:d})."));
        }

        return Task.FromResult(LeaveValidationResult.Pass());
    }
}
