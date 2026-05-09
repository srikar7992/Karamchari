namespace Karamchari.PSA.Domain;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Captures an employee's fully-loaded cost at a point in time.
/// CostPerHour is the atomic unit for allocating employee cost to projects.
///
/// Formula:  CostPerHour = MonthlyCTC / StandardMonthlyHours (default 160)
///
/// This snapshot is taken from the latest PayrollLedgerEntry (MonthlyGross)
/// to include all statutory employer contributions (EPF employer share, ESIC,
/// bonus provisions) â€” not just the base salary.
/// </summary>
public sealed class EmployeeCostSnapshot : ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the employee identifier.</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Gets the fully-loaded monthly cost (CTC including employer statutory contributions).
    /// Sourced from PayrollLedgerEntry.MonthlyGross + employer EPF share.
    /// </summary>
    public decimal MonthlyCost { get; private set; }

    /// <summary>
    /// Gets the cost per hour (MonthlyCost / StandardMonthlyHours).
    /// This is the value used for project cost allocation.
    /// </summary>
    public decimal CostPerHour { get; private set; }

    /// <summary>
    /// Gets the standard monthly working hours used for cost normalization.
    /// Defaults to 160 (8h Ã— 20 working days). Overridable per employee for
    /// part-time workers or clients with different SLAs.
    /// </summary>
    public decimal StandardMonthlyHours { get; private set; }

    /// <summary>Gets the year this snapshot applies to.</summary>
    public int Year { get; private set; }

    /// <summary>Gets the month this snapshot applies to.</summary>
    public int Month { get; private set; }

    /// <summary>Gets when this snapshot was computed.</summary>
    public DateTime ComputedAt { get; private set; }

    private EmployeeCostSnapshot() { }

    /// <summary>
    /// Creates a cost snapshot from the employee's monthly CTC.
    /// </summary>
    public static EmployeeCostSnapshot Compute(
        Guid employeeId,
        decimal monthlyCost,
        int year,
        int month,
        decimal standardMonthlyHours = 160m)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(monthlyCost);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(standardMonthlyHours);

        return new EmployeeCostSnapshot
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            MonthlyCost = monthlyCost,
            CostPerHour = Math.Round(monthlyCost / standardMonthlyHours, 4),
            StandardMonthlyHours = standardMonthlyHours,
            Year = year,
            Month = month,
            ComputedAt = DateTime.UtcNow
        };
    }
}
