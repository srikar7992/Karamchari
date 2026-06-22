// -----------------------------------------------------------------------
// <copyright file="PayrollCalculationResult.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.CountryEngine.Results;

/// <summary>
/// The top-level payroll output for a single employee produced by <see cref="IPayrollCountryEngine"/>.
/// Aggregates gross, deductions, and net into a country-neutral structure.
/// </summary>
public sealed record PayrollCalculationResult(
    Guid EmployeeId,
    string EmployeeName,
    decimal GrossPay,
    decimal TotalDeductions,
    decimal NetPay,

    /// <summary>
    /// Earning components keyed by component name (e.g., "BASIC", "HRA", "SPECIAL_ALLOWANCE").
    /// </summary>
    IReadOnlyDictionary<string, decimal> EarningsBreakdown,

    /// <summary>
    /// Statutory and other deductions keyed by program code (e.g., "EPF", "TDS", "LOAN_RECOVERY").
    /// </summary>
    IReadOnlyDictionary<string, decimal> DeductionsBreakdown
);
