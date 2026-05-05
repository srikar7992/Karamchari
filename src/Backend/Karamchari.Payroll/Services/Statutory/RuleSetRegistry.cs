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
    public IReadOnlyList<IStatutoryRuleMetadata> Rules { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FY20262027RuleSet"/> class.
    /// </summary>
    /// <param name="tenantEpfComponentIds">Component IDs forming the EPF wage base (e.g. Basic, DA).</param>
    /// <param name="ptProvider">The professional tax provider.</param>
    /// <param name="projectionService">The TDS income projection service.</param>
    /// <param name="exemptionCalculator">The HRA/80C exemption calculator.</param>
    /// <param name="taxSlabProvider">The income tax slab provider.</param>
    /// <param name="declarationRepository">The IT declaration repository.</param>
    /// <param name="tenantBasicComponentIds">Component IDs forming the Basic+DA base for HRA exemption.</param>
    /// <param name="tenantHraComponentIds">Component IDs for the HRA element in the CTC template.</param>
    public FY20262027RuleSet(
        List<Guid> tenantEpfComponentIds,
        IProfessionalTaxProvider ptProvider,
        IIncomeProjectionService projectionService,
        IExemptionCalculator exemptionCalculator,
        ITaxSlabProvider taxSlabProvider,
        IITDeclarationRepository declarationRepository,
        List<Guid>? tenantBasicComponentIds = null,
        List<Guid>? tenantHraComponentIds = null)
    {
        Rules = new List<IStatutoryRuleMetadata>
        {
            new EpfStatutoryRule(tenantEpfComponentIds),
            new EsicStatutoryRule(),
            new ProfessionalTaxRule(ptProvider),
            new TdsStatutoryRule(
                projectionService,
                exemptionCalculator,
                taxSlabProvider,
                declarationRepository,
                tenantBasicComponentIds,
                tenantHraComponentIds)
        };
    }
}
