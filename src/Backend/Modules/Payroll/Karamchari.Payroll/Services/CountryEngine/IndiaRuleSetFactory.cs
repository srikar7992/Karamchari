// -----------------------------------------------------------------------
// <copyright file="IndiaRuleSetFactory.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Services.CountryEngine;

using Karamchari.Payroll.Domain.CountryEngine;
using Karamchari.Payroll.Services.Statutory;

/// <summary>
/// Creates the India statutory rule set for a given financial year.
/// As new FY rule sets are introduced (FY2027-28, FY2028-29, etc.), add a mapping here.
/// The factory is injected into <see cref="IndiaPayrollEngine"/> so rule-set selection
/// is driven by <see cref="PayrollContext.EffectiveDate"/>, not by the current system date.
/// </summary>
public sealed class IndiaRuleSetFactory : IStatutoryRuleSetFactory
{
    private readonly IProfessionalTaxProvider _ptProvider;
    private readonly IIncomeProjectionService _projectionService;
    private readonly IExemptionCalculator _exemptionCalculator;
    private readonly ITaxSlabProvider _taxSlabProvider;
    private readonly IITDeclarationRepository _declarationRepository;

    public IndiaRuleSetFactory(
        IProfessionalTaxProvider ptProvider,
        IIncomeProjectionService projectionService,
        IExemptionCalculator exemptionCalculator,
        ITaxSlabProvider taxSlabProvider,
        IITDeclarationRepository declarationRepository)
    {
        _ptProvider = ptProvider;
        _projectionService = projectionService;
        _exemptionCalculator = exemptionCalculator;
        _taxSlabProvider = taxSlabProvider;
        _declarationRepository = declarationRepository;
    }

    /// <inheritdoc/>
    public IStatutoryRuleSet Create(
        FinancialYear year,
        IReadOnlyList<Guid> epfWageComponentIds,
        IReadOnlyList<Guid>? basicComponentIds = null,
        IReadOnlyList<Guid>? hraComponentIds = null)
    {
        ArgumentNullException.ThrowIfNull(year);

        return year.StartYear switch
        {
            2026 => new FY20262027RuleSet(
                epfWageComponentIds.ToList(),
                _ptProvider,
                _projectionService,
                _exemptionCalculator,
                _taxSlabProvider,
                _declarationRepository,
                basicComponentIds?.ToList(),
                hraComponentIds?.ToList()),

            // When FY2027-28 rules arrive, add:
            // 2027 => new FY20272028RuleSet(...)

            _ => throw new NotSupportedException(
                $"No India payroll rule set available for financial year {year.StartYear}-{year.EndYear}. " +
                $"Register a new FY rule set in {nameof(IndiaRuleSetFactory)}.")
        };
    }
}
