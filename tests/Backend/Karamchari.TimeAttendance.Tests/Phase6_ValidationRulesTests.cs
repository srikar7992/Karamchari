// -----------------------------------------------------------------------
// <copyright file="Phase6_ValidationRulesTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.TimeAttendance.Services.Validation;
using Karamchari.TimeAttendance.Services.Validation.Rules;
using Xunit;

/// <summary>Phase 6 — Validation Rules: positive, negative, boundary per rule.</summary>
public sealed class Phase6_ValidationRulesTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static LeaveValidationContext Ctx(
        LeaveRequest request,
        LeavePolicyRules? rules = null,
        decimal balance = 100,
        IReadOnlyList<DateOnly>? holidays = null,
        IReadOnlyList<LeaveRequest>? activeRequests = null,
        IReadOnlyList<LeaveBlackoutPeriod>? blackouts = null)
    {
        return new LeaveValidationContext
        {
            Request = request,
            PolicyRules = rules ?? new LeavePolicyRules { AnnualAllowance = 18 },
            CurrentBalance = balance,
            HolidayDates = holidays ?? [],
            TeamCalendar = [],
            EmployeeActiveRequests = activeRequests ?? [],
            EmployeeTimeZoneId = "UTC",
            TodayInEmployeeZone = Today,
            BlackoutPeriods = blackouts ?? [],
        };
    }

    private static LeaveRequest Req(DateOnly start, DateOnly end, double days = -1)
    {
        if (days < 0) days = (end.DayNumber - start.DayNumber) + 1;
        return LeaveRequest.Create(Guid.NewGuid(), Guid.NewGuid(), start, end, days);
    }

    // ── Balance Rule ──────────────────────────────────────────────────────

    [Fact]
    public async Task Balance_Positive_SufficientBalance_Passes()
    {
        var rule = new BalanceValidationRule();
        var result = await rule.ValidateAsync(Ctx(Req(Today, Today.AddDays(4), 5), balance: 10));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Balance_Negative_InsufficientBalance_Fails()
    {
        var rule = new BalanceValidationRule();
        var result = await rule.ValidateAsync(Ctx(Req(Today, Today.AddDays(9), 10), balance: 5));
        result.IsValid.Should().BeFalse();
        result.Severity.Should().Be(ValidationSeverity.Error);
    }

    [Fact]
    public async Task Balance_Boundary_ExactlyEnoughBalance_Passes()
    {
        var rule = new BalanceValidationRule();
        var result = await rule.ValidateAsync(Ctx(Req(Today, Today.AddDays(4), 5), balance: 5));
        result.IsValid.Should().BeTrue();
    }

    // ── Notice Period Rule ────────────────────────────────────────────────

    [Fact]
    public async Task NoticePeriod_Positive_SufficientAdvance_Passes()
    {
        var rule = new NoticePeriodRule();
        var rules = new LeavePolicyRules { NoticeRequiredDays = 2 };
        var start = Today.AddDays(5);
        var result = await rule.ValidateAsync(Ctx(Req(start, start.AddDays(2)), rules: rules));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NoticePeriod_Negative_InsufficientAdvance_Fails()
    {
        var rule = new NoticePeriodRule();
        var rules = new LeavePolicyRules { NoticeRequiredDays = 5 };
        var start = Today.AddDays(2);
        var result = await rule.ValidateAsync(Ctx(Req(start, start.AddDays(2)), rules: rules));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NoticePeriod_Boundary_ExactlyAtLimit_Passes()
    {
        var rule = new NoticePeriodRule();
        var rules = new LeavePolicyRules { NoticeRequiredDays = 3 };
        var start = Today.AddDays(3);
        var result = await rule.ValidateAsync(Ctx(Req(start, start), rules: rules));
        result.IsValid.Should().BeTrue();
    }

    // ── Consecutive Days Rule ─────────────────────────────────────────────

    [Fact]
    public async Task ConsecutiveDays_Positive_WithinLimit_Passes()
    {
        var rule = new ConsecutiveDaysRule();
        var rules = new LeavePolicyRules { MaxConsecutiveDays = 10 };
        var result = await rule.ValidateAsync(Ctx(Req(Today, Today.AddDays(4), 5), rules: rules));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ConsecutiveDays_Negative_ExceedsLimit_Fails()
    {
        var rule = new ConsecutiveDaysRule();
        var rules = new LeavePolicyRules { MaxConsecutiveDays = 3 };
        var result = await rule.ValidateAsync(Ctx(Req(Today, Today.AddDays(9), 10), rules: rules));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ConsecutiveDays_Boundary_ExactlyAtMax_Passes()
    {
        var rule = new ConsecutiveDaysRule();
        var rules = new LeavePolicyRules { MaxConsecutiveDays = 5 };
        var result = await rule.ValidateAsync(Ctx(Req(Today, Today.AddDays(4), 5), rules: rules));
        result.IsValid.Should().BeTrue();
    }

    // ── Retroactive Leave Rule ────────────────────────────────────────────

    [Fact]
    public async Task Retroactive_Positive_WithinLimit_Passes()
    {
        var rule = new RetroactiveLeaveRule();
        var rules = new LeavePolicyRules { MaxRetroactiveDays = 3 };
        var start = Today.AddDays(-2);
        var result = await rule.ValidateAsync(Ctx(Req(start, start), rules: rules));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Retroactive_Negative_BeyondLimit_Fails()
    {
        var rule = new RetroactiveLeaveRule();
        var rules = new LeavePolicyRules { MaxRetroactiveDays = 2 };
        var start = Today.AddDays(-5);
        var result = await rule.ValidateAsync(Ctx(Req(start, start), rules: rules));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Retroactive_Boundary_ExactlyAtLimit_Passes()
    {
        var rule = new RetroactiveLeaveRule();
        var rules = new LeavePolicyRules { MaxRetroactiveDays = 2 };
        var start = Today.AddDays(-2);
        var result = await rule.ValidateAsync(Ctx(Req(start, start), rules: rules));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Retroactive_ZeroLimit_FutureDatesAllowed()
    {
        var rule = new RetroactiveLeaveRule();
        var rules = new LeavePolicyRules { MaxRetroactiveDays = 0 };
        var start = Today.AddDays(2);
        var result = await rule.ValidateAsync(Ctx(Req(start, start), rules: rules));
        result.IsValid.Should().BeTrue();
    }

    // ── Blackout Rule ─────────────────────────────────────────────────────

    [Fact]
    public async Task Blackout_Positive_OutsideBlackout_Passes()
    {
        var rule = new BlackoutRule();
        var blackout = new LeaveBlackoutPeriod { TenantId = "t1", Name = "Year End", StartDate = Today.AddDays(30), EndDate = Today.AddDays(45) };
        var result = await rule.ValidateAsync(Ctx(Req(Today, Today.AddDays(4)), blackouts: [blackout]));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Blackout_Negative_InsideBlackout_Fails()
    {
        var rule = new BlackoutRule();
        var blackout = new LeaveBlackoutPeriod { TenantId = "t1", Name = "Year End", StartDate = Today, EndDate = Today.AddDays(10) };
        var result = await rule.ValidateAsync(Ctx(Req(Today, Today.AddDays(2)), blackouts: [blackout]));
        result.IsValid.Should().BeFalse();
    }

    // ── Sandwich Rule ─────────────────────────────────────────────────────

    [Fact]
    public async Task Sandwich_AllowHolidaySandwich_True_Passes()
    {
        var rule = new SandwichRule();
        var friday = GetNext(DayOfWeek.Friday);
        var monday = friday.AddDays(3);
        var rules = new LeavePolicyRules { AllowHolidaySandwich = true };
        IReadOnlyList<DateOnly> holidays = [friday.AddDays(1), friday.AddDays(2)];

        var ctx = new LeaveValidationContext
        {
            Request = Req(monday, monday),
            PolicyRules = rules,
            CurrentBalance = 100,
            HolidayDates = holidays,
            TeamCalendar = [],
            EmployeeActiveRequests = [Req(friday, friday)],
            EmployeeTimeZoneId = "UTC",
            TodayInEmployeeZone = friday.AddDays(-5),
            BlackoutPeriods = [],
        };
        var result = await rule.ValidateAsync(ctx);
        result.IsValid.Should().BeTrue("sandwich allowed by policy");
    }

    [Fact]
    public async Task Sandwich_AllowHolidaySandwich_False_WithSandwich_Fails()
    {
        var rule = new SandwichRule();
        var friday = GetNext(DayOfWeek.Friday);
        var monday = friday.AddDays(3);
        var rules = new LeavePolicyRules { AllowHolidaySandwich = false };
        IReadOnlyList<DateOnly> holidays = [friday.AddDays(1), friday.AddDays(2)];

        var ctx = new LeaveValidationContext
        {
            Request = Req(monday, monday),
            PolicyRules = rules,
            CurrentBalance = 100,
            HolidayDates = holidays,
            TeamCalendar = [],
            EmployeeActiveRequests = [Req(friday, friday)],
            EmployeeTimeZoneId = "UTC",
            TodayInEmployeeZone = friday.AddDays(-5),
            BlackoutPeriods = [],
        };
        var result = await rule.ValidateAsync(ctx);
        result.IsValid.Should().BeFalse("sandwich blocked by policy");
    }

    // ── Prefix/Suffix Rule ────────────────────────────────────────────────

    [Fact]
    public async Task PrefixSuffix_AllowPrefixSuffix_True_FridayLeave_SaturdayHoliday_Passes()
    {
        var rule = new PrefixSuffixRule();
        var friday = GetNext(DayOfWeek.Friday);
        var rules = new LeavePolicyRules { AllowPrefixSuffix = true };
        IReadOnlyList<DateOnly> holidays = [friday.AddDays(1)];
        var result = await rule.ValidateAsync(Ctx(Req(friday, friday), rules: rules, holidays: holidays));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PrefixSuffix_AllowPrefixSuffix_False_FridayLeave_SaturdayHoliday_Fails()
    {
        var rule = new PrefixSuffixRule();
        var friday = GetNext(DayOfWeek.Friday);
        var rules = new LeavePolicyRules { AllowPrefixSuffix = false };
        IReadOnlyList<DateOnly> holidays = [friday.AddDays(1)];
        var result = await rule.ValidateAsync(Ctx(Req(friday, friday), rules: rules, holidays: holidays));
        result.IsValid.Should().BeFalse();
    }

    // ── Overlap Rule ──────────────────────────────────────────────────────

    [Fact]
    public async Task Overlap_NoConflict_Passes()
    {
        var rule = new OverlapRule();
        IReadOnlyList<LeaveRequest> existing = [Req(Today.AddDays(10), Today.AddDays(14))];
        var result = await rule.ValidateAsync(Ctx(Req(Today, Today.AddDays(4)), activeRequests: existing));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Overlap_ConflictExists_Fails()
    {
        var rule = new OverlapRule();
        IReadOnlyList<LeaveRequest> existing = [Req(Today, Today.AddDays(4))];
        var result = await rule.ValidateAsync(Ctx(Req(Today, Today.AddDays(2)), activeRequests: existing));
        result.IsValid.Should().BeFalse();
    }

    // ── Validation Engine — Collect All Failures ──────────────────────────

    [Fact]
    public async Task Engine_CollectsAllFailures_NoEarlyExit()
    {
        var rules = new LeavePolicyRules { AnnualAllowance = 5, NoticeRequiredDays = 10 };
        var start = Today.AddDays(1);
        var ctx = Ctx(Req(start, start.AddDays(9), 10), rules: rules, balance: 2);
        var engine = new LeaveValidationEngine([new BalanceValidationRule(), new NoticePeriodRule()]);

        var result = await engine.ValidateAsync(ctx);

        result.Failures.Count.Should().BeGreaterOrEqualTo(2, "engine collects all failures");
        result.IsValid.Should().BeFalse();
    }

    private static DateOnly GetNext(DayOfWeek dow)
    {
        var d = Today.AddDays(7); // ensure future
        while (d.DayOfWeek != dow) d = d.AddDays(1);
        return d;
    }
}
