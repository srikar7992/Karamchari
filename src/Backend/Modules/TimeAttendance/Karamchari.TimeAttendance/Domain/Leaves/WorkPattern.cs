// -----------------------------------------------------------------------
// <copyright file="WorkPattern.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Leaves;

/// <summary>
/// Defines an employee's working pattern. Required for blue-collar shift-aware leave.
/// Examples:
///   IT employee: Mon-Fri (standard)
///   Factory: Tue-Sat (weekly off = Sun+Mon)
///   Security: 4 days on / 2 days off (rotational, anchored to a date)
///   Night shift: 22:00-06:00 (spans calendar boundary)
/// </summary>
public sealed class WorkPattern
{
    /// <summary>Working days for fixed-schedule employees. Empty if IsRotational = true.</summary>
    public HashSet<DayOfWeek> WorkingDays { get; init; } = [];

    public bool IsRotational { get; init; }

    /// <summary>Rotational: consecutive working days in cycle (e.g. 4).</summary>
    public int? RotationOnDays { get; init; }

    /// <summary>Rotational: consecutive off days in cycle (e.g. 2).</summary>
    public int? RotationOffDays { get; init; }

    /// <summary>Rotational: reference date that anchors the rotation cycle.</summary>
    public DateOnly? RotationAnchorDate { get; init; }

    /// <summary>Standard Mon-Fri, Sat-Sun off.</summary>
    public static WorkPattern Standard { get; } = new()
    {
        WorkingDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                       DayOfWeek.Thursday, DayOfWeek.Friday]
    };

    /// <summary>Returns true if the given date is a working day for this pattern.</summary>
    public bool IsWorkingDay(DateOnly date)
    {
        if (IsRotational)
            return IsRotationalWorkingDay(date);

        return WorkingDays.Contains(date.DayOfWeek);
    }

    private bool IsRotationalWorkingDay(DateOnly date)
    {
        if (!RotationOnDays.HasValue || !RotationOffDays.HasValue || !RotationAnchorDate.HasValue)
            return false;

        int cycleLength = RotationOnDays.Value + RotationOffDays.Value;
        int daysSinceAnchor = date.DayNumber - RotationAnchorDate.Value.DayNumber;
        int positionInCycle = ((daysSinceAnchor % cycleLength) + cycleLength) % cycleLength;
        return positionInCycle < RotationOnDays.Value;
    }
}
