// -----------------------------------------------------------------------
// <copyright file="Phase_Attendance_ShiftDefinitionTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Shifts;
using Xunit;

namespace Karamchari.TimeAttendance.Tests;

public sealed class ShiftDefinitionTests
{
    private const string Tenant = "tenant-1";

    // ─── Fixed shift ──────────────────────────────────────────────────────────

    [Fact]
    public void Fixed_shift_creates_with_correct_category()
    {
        var shift = ShiftDefinition.CreateFixed(Tenant, "Morning", "M1",
            new TimeOnly(9, 0), new TimeOnly(18, 0));

        shift.Category.Should().Be(ShiftCategory.Fixed);
        shift.StartTime.Should().Be(new TimeOnly(9, 0));
        shift.EndTime.Should().Be(new TimeOnly(18, 0));
        shift.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Overnight_fixed_shift_detects_IsOvernight()
    {
        var shift = ShiftDefinition.CreateFixed(Tenant, "Night", "N1",
            new TimeOnly(22, 0), new TimeOnly(6, 0));

        shift.IsOvernight.Should().BeTrue();
        shift.IsNightShift.Should().BeTrue();
    }

    [Fact]
    public void Day_shift_is_not_overnight_or_night()
    {
        var shift = ShiftDefinition.CreateFixed(Tenant, "Day", "D1",
            new TimeOnly(9, 0), new TimeOnly(18, 0));

        shift.IsOvernight.Should().BeFalse();
        shift.IsNightShift.Should().BeFalse();
    }

    [Fact]
    public void Night_differential_multiplier_must_be_at_least_one()
    {
        var act = () => ShiftDefinition.CreateFixed(Tenant, "X", "X",
            new TimeOnly(22, 0), new TimeOnly(6, 0),
            nightDifferentialMultiplier: 0.9m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Auto_break_stores_deduction_and_trigger()
    {
        var shift = ShiftDefinition.CreateFixed(Tenant, "M", "M",
            new TimeOnly(9, 0), new TimeOnly(18, 0),
            autoBreakDeductionMinutes: 30,
            autoBreakTriggerMinutes: 360);

        shift.AutoBreakDeductionMinutes.Should().Be(30);
        shift.AutoBreakTriggerMinutes.Should().Be(360);
    }

    // ─── Flexible shift ───────────────────────────────────────────────────────

    [Fact]
    public void Flex_shift_stores_window_and_min_hours()
    {
        var shift = ShiftDefinition.CreateFlexible(Tenant, "Flex", "FL",
            new TimeOnly(7, 0), new TimeOnly(20, 0),
            minHoursPerDay: 8);

        shift.Category.Should().Be(ShiftCategory.Flexible);
        shift.FlexWindowStart.Should().Be(new TimeOnly(7, 0));
        shift.FlexWindowEnd.Should().Be(new TimeOnly(20, 0));
        shift.MinHoursPerDay.Should().Be(8);
        shift.LateArrivalGraceMinutes.Should().Be(0);
    }

    [Fact]
    public void Flex_shift_min_hours_must_be_at_least_one()
    {
        var act = () => ShiftDefinition.CreateFlexible(Tenant, "F", "F",
            new TimeOnly(7, 0), new TimeOnly(20, 0), minHoursPerDay: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ─── Rotating shift ───────────────────────────────────────────────────────

    [Fact]
    public void Rotating_shift_stores_cycle_days()
    {
        var shift = ShiftDefinition.CreateRotating(Tenant, "Rotating", "R1",
            new TimeOnly(6, 0), new TimeOnly(14, 0),
            rotationCycleDays: 14);

        shift.Category.Should().Be(ShiftCategory.Rotating);
        shift.RotationCycleDays.Should().Be(14);
    }

    [Fact]
    public void Rotating_shift_cycle_must_be_at_least_two_days()
    {
        var act = () => ShiftDefinition.CreateRotating(Tenant, "R", "R",
            new TimeOnly(6, 0), new TimeOnly(14, 0), rotationCycleDays: 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ─── Split shift ──────────────────────────────────────────────────────────

    [Fact]
    public void Split_shift_stores_both_legs()
    {
        var shift = ShiftDefinition.CreateSplit(Tenant, "Split", "SP",
            new TimeOnly(9, 0), new TimeOnly(13, 0),
            new TimeOnly(17, 0), new TimeOnly(21, 0));

        shift.Category.Should().Be(ShiftCategory.Split);
        shift.StartTime.Should().Be(new TimeOnly(9, 0));
        shift.EndTime.Should().Be(new TimeOnly(13, 0));
        shift.SplitStartTime.Should().Be(new TimeOnly(17, 0));
        shift.SplitEndTime.Should().Be(new TimeOnly(21, 0));
    }

    [Fact]
    public void Split_shift_second_leg_must_start_after_first_ends()
    {
        var act = () => ShiftDefinition.CreateSplit(Tenant, "S", "S",
            new TimeOnly(9, 0), new TimeOnly(13, 0),
            new TimeOnly(12, 0), new TimeOnly(16, 0));

        act.Should().Throw<ArgumentException>();
    }

    // ─── On-call shift ────────────────────────────────────────────────────────

    [Fact]
    public void OnCall_shift_has_no_fixed_times()
    {
        var shift = ShiftDefinition.CreateOnCall(Tenant, "On Call", "OC");

        shift.Category.Should().Be(ShiftCategory.OnCall);
        shift.StartTime.Should().BeNull();
        shift.EndTime.Should().BeNull();
    }

    // ─── Deactivate / backward-compat ────────────────────────────────────────

    [Fact]
    public void Deactivate_sets_IsActive_false()
    {
        var shift = ShiftDefinition.Create(Tenant, "X", "X",
            new TimeOnly(9, 0), new TimeOnly(18, 0));
        shift.Deactivate();

        shift.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_restores_IsActive()
    {
        var shift = ShiftDefinition.Create(Tenant, "X", "X",
            new TimeOnly(9, 0), new TimeOnly(18, 0));
        shift.Deactivate();
        shift.Activate();

        shift.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateGrace_rejects_negative_values()
    {
        var shift = ShiftDefinition.Create(Tenant, "X", "X",
            new TimeOnly(9, 0), new TimeOnly(18, 0));

        var act = () => shift.UpdateGrace(-1, 10);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
