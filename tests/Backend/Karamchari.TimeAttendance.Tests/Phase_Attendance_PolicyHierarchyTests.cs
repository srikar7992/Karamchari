using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Policy;
using Xunit;

namespace Karamchari.TimeAttendance.Tests;

public sealed class AttendancePolicyHierarchyTests
{
    private const string TenantId = "tenant-policy-1";

    private static AttendancePolicyHierarchy DefaultPolicy()
        => AttendancePolicyHierarchy.CreateOrgDefault(TenantId, "Org Default");

    [Fact]
    public void CreateOrgDefault_sets_sensible_defaults()
    {
        var policy = DefaultPolicy();

        policy.Scope.Should().Be(PolicyScope.Organisation);
        policy.LateGraceMinutes.Should().Be(15);
        policy.FullDayMinMinutes.Should().Be(480);
        policy.HalfDayMinMinutes.Should().Be(240);
        policy.DailyOtTriggerMinutes.Should().Be(480);
        policy.ConsecutiveAbsenceEscalationDays.Should().Be(3);
        policy.CompOffExpiryDays.Should().Be(60);
        policy.AutoDeductLeaveOnAbsent.Should().BeTrue();
    }

    [Theory]
    [InlineData(480, 0, false, AttendanceFinalStatus.Present)]   // 8h on time
    [InlineData(480, 16, false, AttendanceFinalStatus.Late)]     // 8h but 16min late (grace=15)
    [InlineData(480, 130, false, AttendanceFinalStatus.HalfDay)] // 8h but late > half-day threshold
    [InlineData(300, 0, false, AttendanceFinalStatus.HalfDay)]   // 5h < 8h full day, >= 4h half day
    [InlineData(100, 0, false, AttendanceFinalStatus.Absent)]    // too few hours
    [InlineData(0, 0, false, AttendanceFinalStatus.Absent)]      // no show
    [InlineData(300, 0, true, AttendanceFinalStatus.HolidayWork)] // holiday
    [InlineData(0, 0, true, AttendanceFinalStatus.WeekOff)]      // holiday no work
    public void ComputeStatus_produces_correct_result(
        int workedMinutes, int lateMinutes, bool isHoliday, AttendanceFinalStatus expected)
    {
        var policy = DefaultPolicy();
        var status = policy.ComputeStatus(workedMinutes, lateMinutes, isHoliday);
        status.Should().Be(expected);
    }

    [Fact]
    public void ComputeOvertimeMinutes_caps_at_max_daily_ot()
    {
        var policy = DefaultPolicy(); // MaxDailyOtMinutes = 240

        // 12h worked = 4h OT. Cap at 4h.
        var ot = policy.ComputeOvertimeMinutes(720);
        ot.Should().Be(240);
    }

    [Fact]
    public void ComputeOvertimeMinutes_returns_zero_below_trigger()
    {
        var policy = DefaultPolicy(); // DailyOtTriggerMinutes = 480

        var ot = policy.ComputeOvertimeMinutes(400);
        ot.Should().Be(0);
    }

    [Fact]
    public void ShouldAccrueCompOff_true_when_above_trigger()
    {
        var policy = DefaultPolicy(); // CompOffAccrualTriggerMinutes = 240

        policy.ShouldAccrueCompOff(300).Should().BeTrue();
        policy.ShouldAccrueCompOff(240).Should().BeTrue();
        policy.ShouldAccrueCompOff(239).Should().BeFalse();
    }

    [Fact]
    public void CreateOverride_copies_base_values()
    {
        var org = DefaultPolicy();
        var dept = AttendancePolicyHierarchy.CreateOverride(
            TenantId, "Dept Override", PolicyScope.Department, Guid.NewGuid(), org);

        dept.LateGraceMinutes.Should().Be(org.LateGraceMinutes);
        dept.FullDayMinMinutes.Should().Be(org.FullDayMinMinutes);
    }

    [Fact]
    public void Update_with_invalid_half_day_threshold_throws()
    {
        var policy = DefaultPolicy();
        var act = () => policy.Update(
            15, 120,
            fullDayMinMinutes: 240,
            halfDayMinMinutes: 240, // same as full — invalid
            15, 3, 0, 480, 2400, 240, 1.5m, 2.0m, true, 240, 60, false, 3, true, 60, 5);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PunchDedupWindowSeconds_default_is_60()
    {
        var policy = DefaultPolicy();
        policy.PunchDedupWindowSeconds.Should().Be(60);
    }
}
