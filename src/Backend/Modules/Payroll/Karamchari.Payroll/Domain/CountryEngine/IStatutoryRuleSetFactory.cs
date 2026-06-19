// -----------------------------------------------------------------------
// <copyright file="IStatutoryRuleSetFactory.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.CountryEngine;

using Karamchari.Payroll.Services.Statutory;

/// <summary>
/// Creates the statutory rule set appropriate for a given financial year and tenant configuration.
///
/// Decouples rule-set selection from the country engine so that:
/// - Retro-pay runs use the rules active at the original processing date, not today.
/// - Historical audits reproduce exact outputs regardless of when they run.
/// - New financial year rule sets can be added without changing <see cref="IPayrollCountryEngine"/>.
///
/// One factory implementation exists per country (e.g., <c>IndiaRuleSetFactory</c>).
/// </summary>
public interface IStatutoryRuleSetFactory
{
    /// <summary>
    /// Returns the rule set that was active during the specified financial year.
    /// </summary>
    /// <param name="year">The financial year to resolve rules for.</param>
    /// <param name="epfWageComponentIds">Tenant-specific component IDs forming the EPF wage base.</param>
    /// <param name="basicComponentIds">Tenant-specific component IDs for Basic+DA (used for HRA exemption).</param>
    /// <param name="hraComponentIds">Tenant-specific component IDs for the HRA element.</param>
    IStatutoryRuleSet Create(
        FinancialYear year,
        IReadOnlyList<Guid> epfWageComponentIds,
        IReadOnlyList<Guid>? basicComponentIds = null,
        IReadOnlyList<Guid>? hraComponentIds = null);
}
