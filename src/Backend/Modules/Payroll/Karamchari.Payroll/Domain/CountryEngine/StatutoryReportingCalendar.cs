// -----------------------------------------------------------------------
// <copyright file="StatutoryReportingCalendar.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.CountryEngine;

/// <summary>
/// Describes all compliance filing obligations for a country.
/// The scheduling engine reads this calendar to create compliance tasks automatically
/// rather than embedding country-specific due-date logic in application code.
/// </summary>
public sealed record StatutoryReportingCalendar(
    string CountryCode,
    IReadOnlyList<ComplianceObligation> Obligations
);

/// <summary>
/// A single statutory filing obligation with its recurrence schedule.
/// </summary>
public sealed record ComplianceObligation(
    /// <summary>Machine code used to identify this filing (e.g., "TDS_MONTHLY", "EPF_ECR").</summary>
    string Code,

    string DisplayName,
    FilingFrequency Frequency,

    /// <summary>
    /// Calculates the filing due date from the period end date.
    /// Using a value object rather than raw integers (DayOfMonth + MonthOffset)
    /// allows complex jurisdiction rules — last business day, quarter-end + N days,
    /// third Friday — to be expressed without changing this type.
    /// </summary>
    DueDateRule DueDateRule,

    string Description,
    string? LateFilingPenalty
)
{
    /// <summary>Returns the due date for this obligation given the period end date.</summary>
    public DateOnly CalculateDueDate(DateOnly periodEnd) => DueDateRule.Calculate(periodEnd);
}

/// <summary>How often a statutory obligation must be filed.</summary>
public enum FilingFrequency
{
    Monthly = 1,
    Quarterly = 2,
    HalfYearly = 3,
    Annual = 4
}

// ── Due Date Rule hierarchy ──────────────────────────────────────────────────
// Each subtype represents a distinct scheduling pattern. New jurisdictions add
// new subtypes here rather than adding flags or overloaded integer parameters.

/// <summary>Base type for all filing due-date calculation strategies.</summary>
public abstract record DueDateRule
{
    public abstract DateOnly Calculate(DateOnly periodEnd);
}

/// <summary>Due on a fixed day of the month following the period end.</summary>
/// <example>TDS in India: 7th of next month → new FixedDayNextMonth(7)</example>
public sealed record FixedDayNextMonth(int Day) : DueDateRule
{
    public override DateOnly Calculate(DateOnly periodEnd)
    {
        var next = periodEnd.AddMonths(1);
        return new DateOnly(next.Year, next.Month, Day);
    }
}

/// <summary>Due on a fixed day of the month N months after period end.</summary>
/// <example>Quarterly 24Q due Jan 31 (3 months after Q2 Oct end): new FixedDayAfterMonths(31, 3)</example>
public sealed record FixedDayAfterMonths(int Day, int MonthsAfterPeriodEnd) : DueDateRule
{
    public override DateOnly Calculate(DateOnly periodEnd)
    {
        var target = periodEnd.AddMonths(MonthsAfterPeriodEnd);
        var daysInMonth = DateTime.DaysInMonth(target.Year, target.Month);
        return new DateOnly(target.Year, target.Month, Math.Min(Day, daysInMonth));
    }
}

/// <summary>Due on the last calendar day of the month following the period end.</summary>
public sealed record LastDayNextMonth : DueDateRule
{
    public static readonly LastDayNextMonth Instance = new();
    public override DateOnly Calculate(DateOnly periodEnd)
    {
        var next = periodEnd.AddMonths(1);
        return new DateOnly(next.Year, next.Month, DateTime.DaysInMonth(next.Year, next.Month));
    }
}

/// <summary>
/// Due N calendar days after the period end.
/// Useful for countries that express obligations as "within N days of period close."
/// </summary>
public sealed record DaysAfterPeriodEnd(int Days) : DueDateRule
{
    public override DateOnly Calculate(DateOnly periodEnd) => periodEnd.AddDays(Days);
}
