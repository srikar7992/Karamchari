// -----------------------------------------------------------------------
// <copyright file="CountryMetadata.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.CountryEngine;

/// <summary>
/// Static descriptive metadata about a country's payroll environment.
/// Consumed by the UI to drive field visibility, validation, and labels
/// without hardcoding country-specific conditional logic in the frontend.
/// </summary>
public sealed record CountryMetadata(
    /// <summary>ISO 3166-1 alpha-2 country code (e.g., "IN", "AE", "GB", "US").</summary>
    string CountryCode,

    string CountryName,

    /// <summary>ISO 4217 currency code (e.g., "INR", "AED", "GBP", "USD").</summary>
    string DefaultCurrency,

    /// <summary>Payroll frequencies supported by this country's regulations.</summary>
    IReadOnlyList<string> SupportedPayrollFrequencies,

    /// <summary>Tax regimes available for employee election in this country.</summary>
    IReadOnlyList<TaxRegimeOption> SupportedTaxRegimes,

    /// <summary>
    /// Statutory identifiers required per employee (e.g., PAN, UAN, ESIC Number).
    /// The UI renders these as mandatory/optional fields on the payroll profile form.
    /// </summary>
    IReadOnlyList<RequiredEmployeeIdentifier> RequiredEmployeeIdentifiers,

    /// <summary>
    /// Statutory deduction/contribution programs applicable in this country.
    /// Used to label deduction line items on payslips and compliance reports.
    /// </summary>
    IReadOnlyList<StatutoryProgram> SupportedStatutoryPrograms
);

/// <summary>A tax regime selectable by an employee in this country.</summary>
public sealed record TaxRegimeOption(
    string Code,
    string DisplayName,
    string Description
);

/// <summary>
/// An identifier required from employees for statutory compliance in this country.
/// The <see cref="ValidationPattern"/> is a regex pattern for client-side format validation.
/// </summary>
public sealed record RequiredEmployeeIdentifier(
    /// <summary>Machine key used in <c>StatutoryIdentifiers</c> dictionaries (e.g., "PAN", "UAN").</summary>
    string Key,
    string DisplayLabel,
    string HelpText,
    bool IsMandatory,
    string? ValidationPattern
);

/// <summary>A statutory contribution program active in this country.</summary>
public sealed record StatutoryProgram(
    string Code,
    string DisplayName,
    bool IsEmployeeContribution,
    bool IsEmployerContribution,
    string Description
);
