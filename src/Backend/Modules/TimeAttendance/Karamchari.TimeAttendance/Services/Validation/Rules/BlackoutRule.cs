// -----------------------------------------------------------------------
// <copyright file="BlackoutRule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Services.Validation.Rules;

using Karamchari.TimeAttendance.Persistence;

public sealed class BlackoutRule : ILeaveValidationRule
{
    public string RuleName => "BlackoutPeriod";

    public Task<LeaveValidationResult> ValidateAsync(LeaveValidationContext context, CancellationToken ct = default)
    {
        DateOnly start = context.Request.StartDate;
        DateOnly end = context.Request.EndDate;

        LeaveBlackoutPeriod? hit = context.BlackoutPeriods
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
