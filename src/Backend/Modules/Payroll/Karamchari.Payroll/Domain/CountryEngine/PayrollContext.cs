// -----------------------------------------------------------------------
// <copyright file="PayrollContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.CountryEngine;

using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Statutory;

/// <summary>
/// All data required by a country engine to perform payroll calculations.
/// Constructed once per employee per pay period and passed through all engine methods.
/// Contains no live DB references — all values must be resolved before construction.
/// </summary>
public sealed record PayrollContext(
    PayrollProfile Profile,
    CTCBreakdownResult Breakdown,
    FinancialYear FinancialYear,

    /// <summary>The calendar month being processed (1 = January, 12 = December).</summary>
    int PayrollMonth,

    /// <summary>Cumulative gross paid to this employee in the current financial year before this period.</summary>
    decimal YearToDateGross,

    /// <summary>Cumulative TDS deducted in the current financial year before this period.</summary>
    decimal YearToDateTds,

    /// <summary>Total approved investment declarations for the current financial year (80C, 80D, etc.).</summary>
    decimal DeclaredInvestments,

    /// <summary>Monthly rent paid by the employee — required for HRA exemption calculation.</summary>
    decimal MonthlyRentPaid,

    /// <summary>
    /// Salary component IDs that form the EPF wage base (typically Basic + DA).
    /// Sourced from the tenant's salary template configuration.
    /// Required by India's EPF statutory rule; empty for countries without EPF.
    /// </summary>
    IReadOnlyList<Guid> EpfWageComponentIds,

    /// <summary>
    /// Salary component IDs representing Basic + DA, used for HRA exemption calculation.
    /// Required by India's TDS rule (Old Regime); empty for other countries.
    /// </summary>
    IReadOnlyList<Guid> BasicComponentIds,

    /// <summary>
    /// Salary component IDs representing the HRA component.
    /// Required by India's TDS rule (Old Regime); empty for other countries.
    /// </summary>
    IReadOnlyList<Guid> HraComponentIds
);
