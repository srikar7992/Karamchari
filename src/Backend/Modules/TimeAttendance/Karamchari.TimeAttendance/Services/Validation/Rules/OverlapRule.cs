// -----------------------------------------------------------------------
// <copyright file="OverlapRule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Services.Validation.Rules;

using Karamchari.TimeAttendance.Domain.Leaves;

public sealed class OverlapRule : ILeaveValidationRule
{
    public string RuleName => "OverlapCheck";

    public Task<LeaveValidationResult> ValidateAsync(LeaveValidationContext context, CancellationToken ct = default)
    {
        DateOnly start = context.Request.StartDate;
        DateOnly end = context.Request.EndDate;

        bool overlap = context.EmployeeActiveRequests
            .Where(r => r.Id != context.Request.Id)
            .Where(r => r.Status is not (LeaveRequestStatus.Rejected
                or LeaveRequestStatus.Cancelled
                or LeaveRequestStatus.Withdrawn
                or LeaveRequestStatus.Expired))
            .Any(r => r.StartDate <= end && r.EndDate >= start);

        if (overlap)
        {
            return Task.FromResult(LeaveValidationResult.Fail(
                RuleName,
                $"Leave overlaps with an existing active request for {start:d} – {end:d}."));
        }

        return Task.FromResult(LeaveValidationResult.Pass());
    }
}
