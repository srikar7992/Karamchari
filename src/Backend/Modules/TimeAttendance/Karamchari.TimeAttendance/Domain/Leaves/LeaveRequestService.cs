// -----------------------------------------------------------------------
// <copyright file="LeaveRequestService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.TimeAttendance.Domain.Holidays;

/// <summary>
/// Domain service for leave day calculations: full-day, half-day, hourly, proration.
/// </summary>
public sealed class LeaveRequestService
{
    private const double HoursPerDay = 8.0;

    /// <summary>
    /// Calculates working days between two dates, excluding weekends and holidays.
    /// </summary>
    public static double CalculateActualLeaveDays(
        DateOnly start,
        DateOnly end,
        IEnumerable<DateOnly> holidays,
        bool allowHalfDays)
    {
        double totalDays = 0;
        DateOnly current = start;
        ISet<DateOnly> holidaySet = holidays as ISet<DateOnly> ?? new HashSet<DateOnly>(holidays);

        while (current <= end)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday &&
                current.DayOfWeek != DayOfWeek.Sunday &&
                !holidaySet.Contains(current))
            {
                totalDays += 1.0;
            }
            current = current.AddDays(1);
        }

        return totalDays;
    }

    /// <summary>
    /// Prorates annual entitlement for mid-year joins.
    /// E.g. join July 1 (month 7) with annual = 24 → 24 * (6/12) = 12.
    /// </summary>
    public static decimal ProrateAnnualEntitlement(decimal annualDays, DateOnly joinDate, DateOnly yearEnd)
    {
        if (joinDate > yearEnd) return 0;

        int totalMonths = ((yearEnd.Year - joinDate.Year) * 12) + yearEnd.Month - joinDate.Month;

        // Include partial month if joined before the 15th (common convention; configurable in future)
        if (joinDate.Day <= 15) totalMonths++;

        totalMonths = Math.Min(totalMonths, 12);
        return Math.Round(annualDays * totalMonths / 12, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calculates monthly accrual amount for a given month.
    /// Returns 0 if employee was on LOP the full month and policy stops accrual during LOP.
    /// </summary>
    public static decimal CalculateMonthlyAccrual(
        decimal annualDays,
        int year,
        int month,
        bool employeeOnLop,
        bool accrualContinuesDuringLop)
    {
        if (employeeOnLop && !accrualContinuesDuringLop) return 0;
        return Math.Round(annualDays / 12, 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Returns entitlement for employee's tenure using bracket configuration.
    /// </summary>
    public static double GetTenureBasedEntitlement(
        IEnumerable<TenureEntitlementBracket> brackets,
        int completedYears)
    {
        foreach (TenureEntitlementBracket? bracket in brackets.OrderByDescending(b => b.FromYears))
        {
            if (completedYears >= bracket.FromYears &&
                (bracket.ToYears is null || completedYears <= bracket.ToYears))
            {
                return bracket.AnnualDays;
            }
        }
        return 0;
    }

    /// <summary>
    /// Converts hourly leave to fractional days.
    /// </summary>
    public static double HoursToDays(double hours) => hours / HoursPerDay;
}
