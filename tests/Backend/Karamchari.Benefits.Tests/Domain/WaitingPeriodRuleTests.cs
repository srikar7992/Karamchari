// -----------------------------------------------------------------------
// <copyright file="WaitingPeriodRuleTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Tests.Domain;

using Karamchari.Benefits.Domain;

public sealed class WaitingPeriodRuleTests
{
    private static readonly DateOnly HireDate = new(2025, 3, 15);

    // ── Immediate ─────────────────────────────────────────────────────────────

    [Fact]
    public void Immediate_EligibleFrom_returns_hire_date()
    {
        var rule = new WaitingPeriodRule.Immediate();
        Assert.Equal(HireDate, rule.EligibleFrom(HireDate));
    }

    [Fact]
    public void Immediate_ToColumns_type_is_Immediate_days_is_zero()
    {
        var (type, days) = new WaitingPeriodRule.Immediate().ToColumns();
        Assert.Equal("Immediate", type);
        Assert.Equal(0, days);
    }

    // ── FixedDays ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(30, 2025, 4, 14)]
    [InlineData(90, 2025, 6, 13)]
    [InlineData(0, 2025, 3, 15)]
    public void FixedDays_EligibleFrom_adds_correct_days(int days, int year, int month, int day)
    {
        var rule = new WaitingPeriodRule.FixedDays(days);
        Assert.Equal(new DateOnly(year, month, day), rule.EligibleFrom(HireDate));
    }

    [Fact]
    public void FixedDays_ToColumns_round_trips()
    {
        var (type, days) = new WaitingPeriodRule.FixedDays(30).ToColumns();
        Assert.Equal("FixedDays", type);
        Assert.Equal(30, days);
    }

    // ── FirstOfMonthAfterDays ─────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 2025, 4, 1)]   // hire Mar 15, +0 days = Mar 15 → first of April
    [InlineData(17, 2025, 4, 1)]  // hire Mar 15, +17 days = Apr 1 → stays Apr 1 (already 1st)
    [InlineData(18, 2025, 5, 1)]  // hire Mar 15, +18 days = Apr 2 → first of May
    [InlineData(30, 2025, 5, 1)]  // hire Mar 15, +30 days = Apr 14 → first of May
    public void FirstOfMonthAfterDays_EligibleFrom_returns_first_of_correct_month(
        int waitDays, int year, int month, int day)
    {
        var rule = new WaitingPeriodRule.FirstOfMonthAfterDays(waitDays);
        Assert.Equal(new DateOnly(year, month, day), rule.EligibleFrom(HireDate));
    }

    [Fact]
    public void FirstOfMonthAfterDays_ToColumns_round_trips()
    {
        var (type, days) = new WaitingPeriodRule.FirstOfMonthAfterDays(30).ToColumns();
        Assert.Equal("FirstOfMonthAfterDays", type);
        Assert.Equal(30, days);
    }

    // ── ProbationComplete ─────────────────────────────────────────────────────

    [Fact]
    public void ProbationComplete_EligibleFrom_adds_probation_days()
    {
        var rule = new WaitingPeriodRule.ProbationComplete(90);
        Assert.Equal(HireDate.AddDays(90), rule.EligibleFrom(HireDate));
    }

    [Fact]
    public void ProbationComplete_ToColumns_round_trips()
    {
        var (type, days) = new WaitingPeriodRule.ProbationComplete(90).ToColumns();
        Assert.Equal("ProbationComplete", type);
        Assert.Equal(90, days);
    }

    // ── FromColumns round-trip ────────────────────────────────────────────────

    [Theory]
    [InlineData("Immediate", 0)]
    [InlineData("FixedDays", 30)]
    [InlineData("FirstOfMonthAfterDays", 30)]
    [InlineData("ProbationComplete", 90)]
    public void FromColumns_rehydrates_correct_subtype(string type, int days)
    {
        var rule = WaitingPeriodRule.FromColumns(type, days);
        var (rt, rd) = rule.ToColumns();
        Assert.Equal(type, rt);
        Assert.Equal(days, rd);
    }

    [Fact]
    public void FromColumns_throws_for_unknown_type()
    {
        Assert.Throws<ArgumentException>(() =>
            WaitingPeriodRule.FromColumns("UnknownType", 0));
    }
}
