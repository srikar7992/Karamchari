// -----------------------------------------------------------------------
// <copyright file="IAssignmentConstraint.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Rostering.Constraints;

public interface IAssignmentConstraint
{
    string Name { get; }
    ConstraintResult Check(RosterShift shift, AssignmentContext context);
}
