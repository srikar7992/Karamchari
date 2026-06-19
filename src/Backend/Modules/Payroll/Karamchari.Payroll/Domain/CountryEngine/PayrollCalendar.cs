// -----------------------------------------------------------------------
// <copyright file="PayrollCalendar.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.CountryEngine;

/// <summary>
/// Defines the structural rules for payroll periods in a country.
/// Used when creating pay periods and validating payroll schedules.
/// </summary>
public sealed record PayrollCalendar(
    string CountryCode,

    /// <summary>Payroll frequencies permitted in this country (e.g., "Monthly", "Weekly", "Fortnightly").</summary>
    IReadOnlyList<string> SupportedFrequencies,

    string DefaultFrequency,

    /// <summary>Default day of month on which payroll processing runs (1-28).</summary>
    int DefaultProcessingDayOfMonth,

    /// <summary>The month and day on which the financial year starts (e.g., April 1 for India).</summary>
    DateOnly FinancialYearStart,

    /// <summary>Names of weekend days used to exclude non-working days from period calculations.</summary>
    IReadOnlyList<string> WeekendDays,

    /// <summary>
    /// Maximum number of pay periods allowed to be open simultaneously.
    /// Prevents accidental parallel runs.
    /// </summary>
    int MaxOpenPeriods
);
