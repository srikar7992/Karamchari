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
    /// <summary>Machine code used to identify this filing (e.g., "TDS_MONTHLY", "EPF_ECR", "24Q").</summary>
    string Code,

    string DisplayName,
    FilingFrequency Frequency,

    /// <summary>
    /// Day of the month the filing is due.
    /// Combined with <see cref="DueMonthOffset"/> to calculate the absolute due date.
    /// </summary>
    int DueDayOfMonth,

    /// <summary>
    /// Number of months after the period end by which this filing is due.
    /// 0 = same month as period end, 1 = next month, 4 = quarterly + 1 month, etc.
    /// </summary>
    int DueMonthOffset,

    string Description,

    /// <summary>Penalty description for late filing, shown in compliance dashboards.</summary>
    string? LateFilingPenalty
);

/// <summary>How often a statutory obligation must be filed.</summary>
public enum FilingFrequency
{
    Monthly = 1,
    Quarterly = 2,
    HalfYearly = 3,
    Annual = 4
}
