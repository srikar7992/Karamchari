// -----------------------------------------------------------------------
// <copyright file="LeaveConstraint.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Rostering.Constraints;

public sealed class LeaveConstraint : IAssignmentConstraint
{
    public string Name => "Leave";

    public ConstraintResult Check(RosterShift shift, AssignmentContext context)
    {
        LeaveWindow? conflict = context.ApprovedLeaves.FirstOrDefault(
            l => shift.WorkDate >= l.Start && shift.WorkDate <= l.End);

        return conflict is null
            ? ConstraintResult.Allow()
            : ConstraintResult.Deny("LEAVE_CONFLICT",
                $"Employee has approved leave from {conflict.Start} to {conflict.End}.");
    }
}
