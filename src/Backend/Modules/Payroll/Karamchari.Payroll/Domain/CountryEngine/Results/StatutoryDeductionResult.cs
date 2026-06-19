// -----------------------------------------------------------------------
// <copyright file="StatutoryDeductionResult.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.CountryEngine.Results;

/// <summary>
/// The output of the statutory deduction pipeline for a single employee in a single pay period.
/// Keys in <see cref="Deductions"/> match the <see cref="CountryMetadata.SupportedStatutoryPrograms"/> codes.
/// </summary>
public sealed record StatutoryDeductionResult(
    decimal NetPay,

    /// <summary>
    /// Each statutory deduction computed during the pipeline run.
    /// Keys are statutory program codes (e.g., "EPF", "ESIC", "PT", "TDS").
    /// Only applicable deductions are present; non-applicable rules produce no entry.
    /// </summary>
    IReadOnlyDictionary<string, decimal> Deductions
)
{
    public decimal TotalDeductions => Deductions.Values.Sum();
}
