// -----------------------------------------------------------------------
// <copyright file="TaxCalculationResult.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.CountryEngine.Results;

/// <summary>
/// The result of a tax calculation for a single employee.
/// This is a synchronous estimate based on declared investments and projected income.
/// The full TDS including approved IT declarations is computed in <see cref="StatutoryDeductionResult"/>.
/// </summary>
public sealed record TaxCalculationResult(
    Guid EmployeeId,
    decimal TaxableIncome,
    decimal AnnualTaxLiability,
    decimal TdsThisMonth,
    decimal YearToDateTdsDeducted,
    string TaxRegime,

    /// <summary>References the TaxRuleVersion used, enabling deterministic reruns.</summary>
    string RuleVersionId
);
