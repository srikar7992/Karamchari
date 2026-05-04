namespace Karamchari.Payroll.Services.Statutory;

using Karamchari.Payroll.Services.Statutory.Rules;

/// <summary>
/// Registry of statutory rules for the FY 2026-2027.
/// </summary>
public sealed class FY20262027RuleSet : IStatutoryRuleSet
{
    /// <inheritdoc/>
    public FinancialYear Year => new(2026, 2027);

    /// <inheritdoc/>
    public IReadOnlyList<IStatutoryRule> Rules { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FY20262027RuleSet"/> class.
    /// </summary>
    /// <param name="tenantEpfComponentIds">The specific component IDs for EPF calculation for this tenant.</param>
    /// <param name="ptProvider">The PT provider.</param>
    /// <param name="projectionService">The TDS projection service.</param>
    /// <param name="exemptionCalculator">The tax exemption calculator.</param>
    /// <param name="taxSlabProvider">The tax slab provider.</param>
    /// <param name="declarationRepository">The IT declaration repository.</param>
    public FY20262027RuleSet(
        List<Guid> tenantEpfComponentIds, 
        IProfessionalTaxProvider ptProvider,
        IIncomeProjectionService projectionService,
        IExemptionCalculator exemptionCalculator,
        ITaxSlabProvider taxSlabProvider,
        IITDeclarationRepository declarationRepository)
    {
        Rules = new List<IStatutoryRule>
        {
            new EpfStatutoryRule(tenantEpfComponentIds),
            new EsicStatutoryRule(),
            new ProfessionalTaxRule(ptProvider),
            new TdsStatutoryRule(projectionService, exemptionCalculator, taxSlabProvider, declarationRepository)
        };
    }
}
