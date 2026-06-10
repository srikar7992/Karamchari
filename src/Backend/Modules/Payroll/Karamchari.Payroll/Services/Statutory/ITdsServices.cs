// -----------------------------------------------------------------------
// <copyright file="ITdsServices.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Services.Statutory;

using Karamchari.Payroll.Domain.Statutory;

/// <summary>
/// Defines a service for projecting annual income based on YTD actuals and current salary.
/// </summary>
public interface IIncomeProjectionService
{
    /// <summary>
    /// Calculates the projected annual gross income.
    /// </summary>
    Task<AnnualProjection> ProjectAnnualIncomeAsync(StatutoryContext context);
}

/// <summary>
/// Represents the result of an annual income projection.
/// </summary>
/// <param name="YtdGross">Actual gross income received so far in the FY.</param>
/// <param name="YtdTds">Actual TDS already deducted so far in the FY.</param>
/// <param name="ProjectedRemainingGross">Projected gross income for the remaining months.</param>
/// <param name="RemainingMonths">Number of months remaining in the FY (including current).</param>
public record AnnualProjection(
    decimal YtdGross,
    decimal YtdTds,
    decimal ProjectedRemainingGross,
    int RemainingMonths)
{
    /// <summary>Gets the total projected annual gross income.</summary>
    public decimal TotalAnnualGross => YtdGross + ProjectedRemainingGross;
}

/// <summary>
/// Defines a service for calculating tax exemptions (Section 10).
/// </summary>
public interface IExemptionCalculator
{
    /// <summary>
    /// Calculates the annual HRA exemption.
    /// </summary>
    decimal CalculateHraExemption(
        decimal annualBasic,
        decimal annualHra,
        decimal annualRentPaid,
        bool isMetro);
}

/// <summary>
/// Defines a provider for tax slab calculations.
/// </summary>
public interface ITaxSlabProvider
{
    /// <summary>
    /// Calculates the annual tax liability based on net taxable income and regime.
    /// </summary>
    decimal CalculateAnnualTax(decimal taxableIncome, TaxRegime regime);
}
