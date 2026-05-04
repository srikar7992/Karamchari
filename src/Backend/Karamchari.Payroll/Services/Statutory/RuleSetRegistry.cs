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
    public FY20262027RuleSet(List<Guid> tenantEpfComponentIds)
    {
        Rules = new List<IStatutoryRule>
        {
            new EpfStatutoryRule(tenantEpfComponentIds),
            new EsicStatutoryRule()
            // ProfessionalTaxRule would be added here next
        };
    }
}
