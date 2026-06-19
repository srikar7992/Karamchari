// -----------------------------------------------------------------------
// <copyright file="EmployeePayrollSummary.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.CountryEngine.Results;

/// <summary>
/// A country-agnostic per-employee payroll summary passed to
/// <see cref="IPayrollCountryEngine.GenerateComplianceReports"/> for report generation.
/// Country-specific fields (PAN, UAN, ESIC number) are carried in
/// <see cref="StatutoryIdentifiers"/> using the key conventions defined in
/// <see cref="CountryMetadata.RequiredEmployeeIdentifiers"/>.
/// </summary>
public sealed record EmployeePayrollSummary(
    Guid EmployeeId,
    string EmployeeName,

    /// <summary>
    /// Statutory identifiers keyed by <see cref="RequiredEmployeeIdentifier.Key"/>
    /// (e.g., {"PAN": "ABCDE1234F", "UAN": "123456789012", "ESIC_NUMBER": "..."}).
    /// </summary>
    IReadOnlyDictionary<string, string> StatutoryIdentifiers,

    decimal GrossPay,
    decimal NetPay,

    /// <summary>Deduction amounts keyed by statutory program code (e.g., "EPF", "ESIC", "PT", "TDS").</summary>
    IReadOnlyDictionary<string, decimal> Deductions,

    /// <summary>Earning component amounts keyed by component name (e.g., "BASIC", "HRA").</summary>
    IReadOnlyDictionary<string, decimal> Earnings,

    /// <summary>
    /// Country-specific computed values that don't belong in the generic fields.
    /// For India: "EPF_WAGES", "EPS_WAGES", "EDLI_WAGES", "EPF_EPS_DIFF", "NCP_DAYS".
    /// Other countries add their own keys here without changing the type.
    /// </summary>
    IReadOnlyDictionary<string, decimal> ExtendedData
);
