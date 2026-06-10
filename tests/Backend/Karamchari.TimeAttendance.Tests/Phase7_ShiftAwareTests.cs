// -----------------------------------------------------------------------
// <copyright file="Phase7_ShiftAwareTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Services;
using Xunit;

/// <summary>Phase 7 — Shift-Aware Validation: night shift attribution, rotational drift, leap year.</summary>
public sealed class Phase7_ShiftAwareTests
{
    private static WorkPattern Rotational(int onDays, int offDays, DateOnly anchor) =>
        new()
        {
            IsRotational = true,
            RotationOnDays = onDays,
            RotationOffDays = offDays,
            RotationAnchorDate = anchor,
        };

    // ── Night Shift ───────────────────────────────────────────────────────

    [Fact]
    public void NightShift_LeaveAt2300_AttributedToShiftStartDate()
    {
        var shiftStart = new DateTime(2026, 6, 10, 22, 0, 0);
        var shiftEnd = new DateTime(2026, 6, 11, 6, 0, 0);
        var leaveStart = new DateTime(2026, 6, 10, 23, 0, 0);

        var result = ShiftAwareLeaveCalculator.GetEffectiveLeaveDate(shiftStart, shiftEnd, leaveStart);

        result.Should().Be(new DateOnly(2026, 6, 10));
    }

    [Fact]
    public void NightShift_LeaveAt0100_StillAttributedToShiftStartDate()
    {
        var shiftStart = new DateTime(2026, 6, 10, 22, 0, 0);
        var shiftEnd = new DateTime(2026, 6, 11, 6, 0, 0);
        var leaveStart = new DateTime(2026, 6, 11, 1, 0, 0);

        var result = ShiftAwareLeaveCalculator.GetEffectiveLeaveDate(shiftStart, shiftEnd, leaveStart);

        result.Should().Be(new DateOnly(2026, 6, 10),
            "1am on June 11 within the June 10 night shift is attributed to June 10");
    }

    [Fact]
    public void NightShift_LeaveOutsideShift_Throws()
    {
        var shiftStart = new DateTime(2026, 6, 11, 6, 0, 0);
        var shiftEnd = new DateTime(2026, 6, 11, 14, 0, 0);
        var leaveOutside = new DateTime(2026, 6, 11, 15, 0, 0); // after shift end

        var act = () => ShiftAwareLeaveCalculator.GetEffectiveLeaveDate(shiftStart, shiftEnd, leaveOutside);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── Rotational Pattern ────────────────────────────────────────────────

    [Fact]
    public void RotationalPattern_4On2Off_CycleIsCorrect()
    {
        var anchor = new DateOnly(2026, 1, 1);
        var pattern = Rotational(4, 2, anchor);

        pattern.IsWorkingDay(anchor).Should().BeTrue("day 0: on");
        pattern.IsWorkingDay(anchor.AddDays(1)).Should().BeTrue("day 1: on");
        pattern.IsWorkingDay(anchor.AddDays(2)).Should().BeTrue("day 2: on");
        pattern.IsWorkingDay(anchor.AddDays(3)).Should().BeTrue("day 3: on");
        pattern.IsWorkingDay(anchor.AddDays(4)).Should().BeFalse("day 4: off");
        pattern.IsWorkingDay(anchor.AddDays(5)).Should().BeFalse("day 5: off");
        pattern.IsWorkingDay(anchor.AddDays(6)).Should().BeTrue("day 6: back to on");
    }

    [Fact]
    public void RotationalPattern_365DaySimulation_NoDrift()
    {
        var anchor = new DateOnly(2026, 1, 1);
        var pattern = Rotational(4, 2, anchor);
        const int cycleLength = 6;
        const int onDays = 4;

        int totalWorking = 0;
        for (int i = 0; i < 365; i++)
            if (pattern.IsWorkingDay(anchor.AddDays(i))) totalWorking++;

        int expectedWorking = (365 / cycleLength) * onDays + Math.Min(365 % cycleLength, onDays);
        totalWorking.Should().Be(expectedWorking, "no drift over 365 days");
    }

    [Fact]
    public void RotationalPattern_LeapYear_Feb29_DoesNotThrow()
    {
        var anchor = new DateOnly(2028, 1, 1);
        var pattern = Rotational(4, 2, anchor);
        var feb29 = new DateOnly(2028, 2, 29);

        var act = () => pattern.IsWorkingDay(feb29);
        act.Should().NotThrow("Feb 29 must be handled without exception");
    }

    [Fact]
    public void RotationalPattern_NegativeOffset_BeforeAnchor_Correct()
    {
        var anchor = new DateOnly(2026, 6, 10);
        var pattern = Rotational(4, 2, anchor);
        // 1 day before anchor → position in cycle = ((-1 % 6) + 6) % 6 = 5 → off day
        pattern.IsWorkingDay(anchor.AddDays(-1)).Should().BeFalse("1 day before anchor is last off day");
    }

    // ── Fixed Pattern ─────────────────────────────────────────────────────

    [Fact]
    public void FixedPattern_Standard_WeekendNotWorking()
    {
        WorkPattern.Standard.IsWorkingDay(new DateOnly(2026, 6, 13)).Should().BeFalse("Saturday");
        WorkPattern.Standard.IsWorkingDay(new DateOnly(2026, 6, 14)).Should().BeFalse("Sunday");
        WorkPattern.Standard.IsWorkingDay(new DateOnly(2026, 6, 15)).Should().BeTrue("Monday");
    }

    // ── Leave Days Calculation ────────────────────────────────────────────

    [Fact]
    public void CalculateLeaveDays_FixedPattern_ExcludesWeekends()
    {
        var start = new DateOnly(2026, 6, 15); // Monday
        var end = new DateOnly(2026, 6, 21);   // Sunday
        var days = ShiftAwareLeaveCalculator.CalculateLeaveDays(start, end, WorkPattern.Standard, []);
        days.Should().Be(5.0, "Mon-Fri only = 5 working days");
    }

    [Fact]
    public void CalculateLeaveDays_HolidayMidWeek_ExcludesHoliday()
    {
        var start = new DateOnly(2026, 6, 15); // Monday
        var end = new DateOnly(2026, 6, 19);   // Friday
        IReadOnlyList<DateOnly> holidays = [new DateOnly(2026, 6, 17)]; // Wednesday
        var days = ShiftAwareLeaveCalculator.CalculateLeaveDays(start, end, WorkPattern.Standard, holidays);
        days.Should().Be(4.0);
    }

    [Fact]
    public void CalculateLeaveDays_LeapYearFeb29_CountsAsWorkingDay()
    {
        // Feb 29, 2028 is a Tuesday
        var start = new DateOnly(2028, 2, 29);
        var end = new DateOnly(2028, 2, 29);
        var days = ShiftAwareLeaveCalculator.CalculateLeaveDays(start, end, WorkPattern.Standard, []);
        days.Should().Be(1.0);
    }

    // ── Shift Fraction ────────────────────────────────────────────────────

    [Fact]
    public void ShiftFraction_HalfShift_Returns0Point5()
    {
        ShiftAwareLeaveCalculator.CalculateShiftFraction(8.0, 4.0).Should().BeApproximately(0.5, 0.001);
    }

    [Fact]
    public void ShiftFraction_FullShift_Returns1()
    {
        ShiftAwareLeaveCalculator.CalculateShiftFraction(8.0, 8.0).Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void ShiftFraction_ZeroShiftHours_Throws()
    {
        var act = () => ShiftAwareLeaveCalculator.CalculateShiftFraction(0, 4.0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
