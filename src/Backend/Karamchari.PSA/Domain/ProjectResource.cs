namespace Karamchari.PSA.Domain;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Maps an employee to a client project with a time-bound billable rate.
/// This is the core revenue model: rate lookups always use the rate effective on the work date,
/// so mid-project rate changes are reflected correctly in historical invoices.
/// </summary>
public sealed class ProjectResource : ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the employee identifier (soft reference to HR domain).</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>Gets the project this resource is assigned to.</summary>
    public Guid ProjectId { get; private set; }

    /// <summary>
    /// Gets the hourly billable rate charged to the client for this employee on this project.
    /// </summary>
    public decimal BillableRate { get; private set; }

    /// <summary>Gets the currency of the billable rate (e.g., "INR", "USD").</summary>
    public string Currency { get; private set; } = "INR";

    /// <summary>Gets the date from which this rate is effective (inclusive).</summary>
    public DateOnly EffectiveFrom { get; private set; }

    /// <summary>Gets the date until which this rate is effective (inclusive, null = still active).</summary>
    public DateOnly? EffectiveTo { get; private set; }

    /// <summary>
    /// Gets whether hours logged by this resource on this project are billable to the client.
    /// A NonBillable project resource can still log hours for internal tracking.
    /// </summary>
    public bool IsBillable { get; private set; }

    private ProjectResource() { }

    /// <summary>
    /// Assigns an employee to a project at the given rate.
    /// </summary>
    public static ProjectResource Assign(
        Guid employeeId,
        Guid projectId,
        decimal billableRate,
        string currency,
        DateOnly effectiveFrom,
        bool isBillable = true)
    {
        if (employeeId == Guid.Empty) throw new ArgumentException("EmployeeId must not be empty.", nameof(employeeId));
        if (projectId == Guid.Empty) throw new ArgumentException("ProjectId must not be empty.", nameof(projectId));
        if (billableRate < 0) throw new ArgumentOutOfRangeException(nameof(billableRate), "Billable rate cannot be negative.");

        return new ProjectResource
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            ProjectId = projectId,
            BillableRate = billableRate,
            Currency = currency.ToUpperInvariant(),
            EffectiveFrom = effectiveFrom,
            EffectiveTo = null,
            IsBillable = isBillable
        };
    }

    /// <summary>
    /// Ends this rate card on the given date. A new <see cref="ProjectResource"/> should be
    /// created with the revised rate and a new <see cref="EffectiveFrom"/>.
    /// </summary>
    public void ExpireOn(DateOnly endDate)
    {
        if (endDate < EffectiveFrom)
            throw new InvalidOperationException("Expiry date cannot be before effective date.");

        EffectiveTo = endDate;
    }
}
