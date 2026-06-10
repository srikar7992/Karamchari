// -----------------------------------------------------------------------
// <copyright file="OverlapConstraint.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Schedules.Constraints;

/// <summary>
/// Basic constraint to prevent double-booking an employee on the same day.
/// </summary>
public sealed class OverlapConstraint : IShiftConstraint
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name => "OverlapPrevention";

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IEnumerable<ConstraintViolation> Evaluate(Guid employeeId, Guid shiftId, DateOnly workDate, SchedulingContext context)
    {
        if (context.Schedule.Assignments.Any(a => a.EmployeeId == employeeId && a.Date == workDate))
        {
            yield return new ConstraintViolation(
                Name,
                $"Employee already has a shift assignment on {workDate}.",
                ConstraintSeverity.Error);
        }
    }
}
