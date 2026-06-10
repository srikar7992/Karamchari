// -----------------------------------------------------------------------
// <copyright file="SchedulingCandidate.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.TimeAttendance.Domain.Rostering.Constraints;

namespace Karamchari.TimeAttendance.Domain.Rostering.Scheduling;

public sealed class SchedulingCandidate
{
    public Guid EmployeeId { get; init; }
    public IReadOnlyList<EmployeeSkill> Skills { get; init; } = [];
    public bool IsOnLeave { get; init; }
    public bool IsAbsent { get; init; }
    public bool IsUnavailable { get; init; }
    public decimal WeeklyHoursWorked { get; init; }
    public OvertimePolicy OvertimePolicy { get; init; } = OvertimePolicy.Default;

    /// <summary>Night shifts already assigned this measurement window.</summary>
    public int NightShiftsThisWeek { get; init; }

    /// <summary>Weekend shifts (Saturday/Sunday) already assigned this measurement window.</summary>
    public int WeekendShiftsThisWeek { get; init; }

    public int ConsecutiveNightShifts { get; init; }

    public bool IsEligible => !IsOnLeave && !IsAbsent && !IsUnavailable;
}

public sealed class CandidateScore
{
    public Guid EmployeeId { get; init; }
    public double TotalScore { get; init; }
    public double SkillScore { get; init; }
    public double AvailabilityScore { get; init; }

    /// <summary>Composite fairness — weighted average of the three sub-dimensions below.</summary>
    public double FairnessScore { get; init; }

    /// <summary>Hours-load fairness: employees with fewer hours this week rank higher.</summary>
    public double WeeklyHoursFairnessScore { get; init; }

    /// <summary>Night-shift fairness: employees with fewer night shifts this week rank higher.</summary>
    public double NightShiftFairnessScore { get; init; }

    /// <summary>Weekend fairness: employees with fewer weekend shifts this week rank higher.</summary>
    public double WeekendFairnessScore { get; init; }

    public double FatigueScore { get; init; }
}

public sealed class ShiftFillRequest
{
    public Guid RosterShiftId { get; init; }
    public ShiftType ShiftType { get; init; }
    public DateOnly WorkDate { get; init; }
    public IReadOnlyList<SkillRequirement> RequiredSkills { get; init; } = [];
}
