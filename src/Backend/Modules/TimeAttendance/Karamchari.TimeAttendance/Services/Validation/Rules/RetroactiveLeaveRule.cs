// -----------------------------------------------------------------------
// <copyright file="RetroactiveLeaveRule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Services.Validation.Rules;

/// <summary>
/// Validates retroactive (backdated) leave applications.
/// MaxRetroactiveDays = 0 → no backdating allowed.
/// MaxRetroactiveDays = 2 → sick leave can be applied up to 2 days after return (48hr window).
/// </summary>
public sealed class RetroactiveLeaveRule : ILeaveValidationRule
{
    public string RuleName => "RetroactiveLeave";

    public Task<LeaveValidationResult> ValidateAsync(LeaveValidationContext context, CancellationToken ct = default)
    {
        bool isBackdated = context.Request.StartDate < context.TodayInEmployeeZone;
        if (!isBackdated)
            return Task.FromResult(LeaveValidationResult.Pass());

        int maxDays = context.PolicyRules.MaxRetroactiveDays;

        if (maxDays == 0)
        {
            return Task.FromResult(LeaveValidationResult.Fail(
                RuleName,
                "Backdated leave is not permitted under this policy."));
        }

        int daysPast = context.TodayInEmployeeZone.DayNumber - context.Request.StartDate.DayNumber;
        if (daysPast > maxDays)
        {
            return Task.FromResult(LeaveValidationResult.Fail(
                RuleName,
                $"Leave start date is {daysPast} day(s) in the past. Maximum retroactive window is {maxDays} day(s)."));
        }

        return Task.FromResult(LeaveValidationResult.Pass());
    }
}
