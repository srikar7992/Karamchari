// -----------------------------------------------------------------------
// <copyright file="IProfessionalTaxProvider.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Services.Statutory;

using Karamchari.Payroll.Domain.Statutory;

/// <summary>
/// Defines a provider for looking up Professional Tax (PT) amounts.
/// </summary>
public interface IProfessionalTaxProvider
{
    /// <summary>
    /// Gets the tax amount for a specific state, gross salary, and month.
    /// </summary>
    ProfessionalTaxResult GetTaxAmount(string stateCode, decimal monthlyGross, int month, FinancialYear year);
}

/// <summary>
/// Defines the result of a PT lookup.
/// </summary>
/// <param name="Amount">The tax amount.</param>
/// <param name="IsApplicable">Whether PT is applicable.</param>
/// <param name="SlabInfo">A human-readable description of the slab used.</param>
public record ProfessionalTaxResult(decimal Amount, bool IsApplicable, string SlabInfo);

/// <summary>
/// Defines a repository for fetching PT slabs from the database.
/// </summary>
public interface IProfessionalTaxRepository
{
    /// <summary>
    /// Fetches all slabs for a given financial year.
    /// </summary>
    Task<List<ProfessionalTaxSlab>> GetSlabsAsync(FinancialYear year);
}
