namespace Karamchari.Payroll.Services.Statutory.Rules;

/// <summary>
/// Implements the Professional Tax (PT) deduction rule based on state-specific slabs.
/// </summary>
public sealed class ProfessionalTaxRule : IStatutoryRule
{
    private readonly IProfessionalTaxProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfessionalTaxRule"/> class.
    /// </summary>
    /// <param name="provider">The PT provider.</param>
    public ProfessionalTaxRule(IProfessionalTaxProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc/>
    public string Name => "ProfessionalTax";

    /// <inheritdoc/>
    public int ExecutionOrder => 30; // Typically runs after EPF and ESIC

    /// <inheritdoc/>
    public StatutoryResult Apply(StatutoryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var result = _provider.GetTaxAmount(
            context.Profile.StateCode,
            context.Breakdown.MonthlyGross,
            context.PayrollMonth,
            context.Year
        );

        return new StatutoryResult(
            Name,
            result.Amount,
            result.IsApplicable,
            result.SlabInfo
        );
    }
}
