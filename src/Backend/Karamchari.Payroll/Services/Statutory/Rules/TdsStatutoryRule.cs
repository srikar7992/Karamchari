namespace Karamchari.Payroll.Services.Statutory.Rules;

using Karamchari.Payroll.Domain.Statutory;

/// <summary>
/// Implements the Tax Deducted at Source (TDS) calculation rule.
/// This is a complex rule that annualizes income, applies exemptions, and projects tax liability.
/// </summary>
public sealed class TdsStatutoryRule : IAsyncStatutoryRule
{
    private readonly IIncomeProjectionService _projectionService;
    private readonly IExemptionCalculator _exemptionCalculator;
    private readonly ITaxSlabProvider _taxSlabProvider;
    private readonly IITDeclarationRepository _declarationRepository;
    // Component IDs whose monthly values form the Basic + DA base used for HRA exemption.
    // Mirror the same pattern used by EpfStatutoryRule for its wage base.
    private readonly IReadOnlyList<Guid> _basicComponentIds;
    // Component IDs that represent the HRA earnings element in the CTC template.
    private readonly IReadOnlyList<Guid> _hraComponentIds;

    /// <summary>
    /// Initializes a new instance of the <see cref="TdsStatutoryRule"/> class.
    /// </summary>
    /// <param name="projectionService">The income projection service.</param>
    /// <param name="exemptionCalculator">The HRA/80C exemption calculator.</param>
    /// <param name="taxSlabProvider">The tax slab provider.</param>
    /// <param name="declarationRepository">The IT declaration repository.</param>
    /// <param name="basicComponentIds">Component IDs forming the Basic+DA wage base for HRA exemption.</param>
    /// <param name="hraComponentIds">Component IDs for the HRA element in the CTC template.</param>
    public TdsStatutoryRule(
        IIncomeProjectionService projectionService,
        IExemptionCalculator exemptionCalculator,
        ITaxSlabProvider taxSlabProvider,
        IITDeclarationRepository declarationRepository,
        IReadOnlyList<Guid>? basicComponentIds = null,
        IReadOnlyList<Guid>? hraComponentIds = null)
    {
        _projectionService = projectionService ?? throw new ArgumentNullException(nameof(projectionService));
        _exemptionCalculator = exemptionCalculator ?? throw new ArgumentNullException(nameof(exemptionCalculator));
        _taxSlabProvider = taxSlabProvider ?? throw new ArgumentNullException(nameof(taxSlabProvider));
        _declarationRepository = declarationRepository ?? throw new ArgumentNullException(nameof(declarationRepository));
        _basicComponentIds = basicComponentIds ?? Array.Empty<Guid>();
        _hraComponentIds = hraComponentIds ?? Array.Empty<Guid>();
    }

    /// <inheritdoc/>
    public string Name => "TDS";

    /// <inheritdoc/>
    public int ExecutionOrder => 99; // Runs last

    /// <inheritdoc/>
    public async ValueTask<StatutoryResult> ApplyAsync(StatutoryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 1. Get Annual Projection (Asynchronously)
        var projection = await _projectionService.ProjectAnnualIncomeAsync(context);

        // 2. Get Approved IT Declarations (Asynchronously)
        var approvedDeclarations = await _declarationRepository.GetApprovedDeclarationsAsync(context.Profile.EmployeeId, context.Year.StartYear);

        decimal section80C = approvedDeclarations.Where(d => d.Category == "80C").Sum(d => d.ApprovedAmount ?? 0);
        decimal section80D = approvedDeclarations.Where(d => d.Category == "80D").Sum(d => d.ApprovedAmount ?? 0);
        decimal monthlyRent = approvedDeclarations.Where(d => d.Category == "HRA").Max(d => d.ApprovedAmount ?? 0); // Rent is usually a monthly max

        // 3. Calculate HRA exemption using the actual component values from the CTC breakdown.
        //    Previously annualHra was hardcoded to 0 â€” this meant HRA exemption was never
        //    applied even for employees with HRA declarations, causing TDS over-deduction.
        decimal annualBasic = context.GetBaseWage(_basicComponentIds) * 12;
        decimal annualHra = context.GetBaseWage(_hraComponentIds) * 12;
        decimal annualRent = monthlyRent * 12;

        decimal hraExemption = _exemptionCalculator.CalculateHraExemption(annualBasic, annualHra, annualRent, context.Profile.IsMetro);

        // 4. Calculate Net Taxable Income
        decimal netTaxableIncome = projection.TotalAnnualGross - hraExemption;

        if (context.Profile.TaxRegime == TaxRegime.Old)
        {
            netTaxableIncome -= Math.Min(section80C, 150000);
            netTaxableIncome -= section80D;
            // netTaxableIncome -= homeLoanInterest; // Add more as needed
        }

        // Standard Deduction
        netTaxableIncome -= 50000;

        if (netTaxableIncome < 0) netTaxableIncome = 0;

        // 5. Calculate Annual Tax
        decimal annualTaxLiability = _taxSlabProvider.CalculateAnnualTax(netTaxableIncome, context.Profile.TaxRegime);

        // 6. Calculate Monthly TDS
        decimal remainingTax = annualTaxLiability - projection.YtdTds;
        decimal monthlyTds = remainingTax > 0 && projection.RemainingMonths > 0
            ? Math.Round(remainingTax / projection.RemainingMonths, 0, MidpointRounding.AwayFromZero)
            : 0;

        string explanation = $"Projected Annual Tax: â‚¹{annualTaxLiability:N2}. Remaining months: {projection.RemainingMonths}. Net Taxable: â‚¹{netTaxableIncome:N2}.";

        return new StatutoryResult(Name, monthlyTds, true, explanation);
    }
}
