namespace Karamchari.PSA.Domain;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Pre-aggregated monthly rollup of project financials.
/// This is the ONLY table the dashboard queries hit â€” never the raw ledger.
///
/// Updated via atomic MERGE/upsert by <c>ProfitCalculationConsumer</c> on every event.
/// Composite PK: (ProjectId, Year, Month) â€” no duplicates, constant-time lookups.
/// </summary>
public sealed class ProjectMonthlyMetrics : ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the project identifier.</summary>
    public Guid ProjectId { get; private set; }

    /// <summary>Gets the calendar year.</summary>
    public int Year { get; private set; }

    /// <summary>Gets the calendar month (1-12).</summary>
    public int Month { get; private set; }

    /// <summary>Gets the total billable revenue for the month.</summary>
    public decimal TotalRevenue { get; private set; }

    /// <summary>Gets the total allocated employee cost for the month.</summary>
    public decimal TotalCost { get; private set; }

    /// <summary>Gets the computed profit (TotalRevenue âˆ’ TotalCost).</summary>
    public decimal TotalProfit => TotalRevenue - TotalCost;

    /// <summary>Gets the margin percentage.</summary>
    public decimal MarginPercent => TotalRevenue == 0 ? 0 : Math.Round(TotalProfit / TotalRevenue * 100, 2);

    /// <summary>Gets the total billable hours aggregated for the month.</summary>
    public decimal BillableHours { get; private set; }

    /// <summary>Gets the total hours (billable + non-billable) for utilization.</summary>
    public decimal TotalHours { get; private set; }

    /// <summary>
    /// Gets the utilization percentage (BillableHours / TotalHours Ã— 100).
    /// </summary>
    public decimal UtilizationPercent => TotalHours == 0 ? 0 : Math.Round(BillableHours / TotalHours * 100, 2);

    /// <summary>
    /// Gets the burn rate (TotalCost / TotalRevenue).
    /// &gt; 1 = losing money, = 1 = break-even, &lt; 1 = profitable.
    /// </summary>
    public decimal BurnRate => TotalRevenue == 0 ? 0 : Math.Round(TotalCost / TotalRevenue, 4);

    /// <summary>Gets the timestamp of the last aggregation update.</summary>
    public DateTime LastUpdatedAt { get; private set; }

    private ProjectMonthlyMetrics() { }

    /// <summary>
    /// Creates a new monthly metrics row.
    /// </summary>
    public static ProjectMonthlyMetrics Create(
        Guid projectId,
        int year,
        int month,
        decimal revenue,
        decimal cost,
        decimal billableHours,
        decimal totalHours)
    {
        return new ProjectMonthlyMetrics
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Year = year,
            Month = month,
            TotalRevenue = revenue,
            TotalCost = cost,
            BillableHours = billableHours,
            TotalHours = totalHours,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Atomically accumulates new values into the existing aggregation.
    /// Called by the consumer after computing a new profit ledger entry.
    /// </summary>
    public void Accumulate(decimal revenue, decimal cost, decimal billableHours, decimal totalHours)
    {
        TotalRevenue += revenue;
        TotalCost += cost;
        BillableHours += billableHours;
        TotalHours += totalHours;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
