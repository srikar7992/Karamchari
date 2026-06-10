// -----------------------------------------------------------------------
// <copyright file="IShiftConstraint.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.TimeAttendance.Domain.Compliance;
using Karamchari.TimeAttendance.Domain.Shifts;

namespace Karamchari.TimeAttendance.Domain.Schedules.Constraints;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum ConstraintSeverity { Information, Warning, Error }

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record ConstraintViolation(string RuleName, string Message, ConstraintSeverity Severity);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record SchedulingContext(
    WorkforceSchedule Schedule,
    IEnumerable<ShiftDefinition> AllShifts,
    IEnumerable<AttendancePolicy> Policies);

/// <summary>
/// Interface for composable scheduling rules.
/// Prevents "if/else hell" in the scheduling service.
/// </summary>
public interface IShiftConstraint
{
    string Name { get; }
    IEnumerable<ConstraintViolation> Evaluate(Guid employeeId, Guid shiftId, DateOnly workDate, SchedulingContext context);
}
