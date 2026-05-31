namespace Karamchari.Payroll.Services.Statutory.Rules;

/// <summary>
/// Implements the Employee Provident Fund (EPF) deduction rule.
/// </summary>
public sealed class EpfStatutoryRule : IStatutoryRule
{
    private readonly List<Guid> _epfBaseComponentIds;
    private const decimal StatutoryWageCap = 15000m;
    private const decimal EpfRate = 0.12m; // Standard 12%

    /// <summary>
    /// Initializes a new instance of the <see cref="EpfStatutoryRule"/> class.
    /// </summary>
    /// <param name="epfBaseComponentIds">The list of component IDs that form the EPF wage base (e.g. Basic + DA).</param>
    public EpfStatutoryRule(List<Guid> epfBaseComponentIds)
    {
        _epfBaseComponentIds = epfBaseComponentIds ?? new List<Guid>();
    }

    /// <inheritdoc/>
    public string Name => "EPF_Employee";

    /// <inheritdoc/>
    public int ExecutionOrder => 10;

    /// <inheritdoc/>
    public StatutoryResult Apply(StatutoryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        decimal epfBaseWage = context.GetBaseWage(_epfBaseComponentIds);

        // If not opting for Voluntary PF (VPF), cap the base at â‚¹15,000 for statutory deduction
        string explanation = $"12% of wage base (â‚¹{epfBaseWage:N2})";
        if (!context.Profile.OptedForVoluntaryPF && epfBaseWage > StatutoryWageCap)
        {
            epfBaseWage = StatutoryWageCap;
            explanation = $"12% of capped wage base (â‚¹{StatutoryWageCap:N2})";
        }

        // Round to nearest rupee as per EPF standards
        decimal epfDeduction = Math.Round(epfBaseWage * EpfRate, 0, MidpointRounding.AwayFromZero);

        return new StatutoryResult(Name, epfDeduction, true, explanation);
    }
}
