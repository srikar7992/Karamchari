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

    /// <summary>
    /// Initializes a new instance of the <see cref="TdsStatutoryRule"/> class.
    /// </summary>
    public TdsStatutoryRule(
        IIncomeProjectionService projectionService,
        IExemptionCalculator exemptionCalculator,
        ITaxSlabProvider taxSlabProvider,
        IITDeclarationRepository declarationRepository)
    {
        _projectionService = projectionService ?? throw new ArgumentNullException(nameof(projectionService));
        _exemptionCalculator = exemptionCalculator ?? throw new ArgumentNullException(nameof(exemptionCalculator));
        _taxSlabProvider = taxSlabProvider ?? throw new ArgumentNullException(nameof(taxSlabProvider));
        _declarationRepository = declarationRepository ?? throw new ArgumentNullException(nameof(declarationRepository));
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

        // 3. Calculate Exemptions (e.g., HRA)
        decimal annualBasic = context.GetBaseWage(new List<Guid>()) * 12; 
        decimal annualHra = 0m; 
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

        string explanation = $"Projected Annual Tax: ₹{annualTaxLiability:N2}. Remaining months: {projection.RemainingMonths}. Net Taxable: ₹{netTaxableIncome:N2}.";

        return new StatutoryResult(Name, monthlyTds, true, explanation);
    }
}
