// -----------------------------------------------------------------------
// <copyright file="ProjectProfitLedger.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.PSA.Domain;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Records the cost-vs-revenue breakdown for a single (Employee Ã— Project Ã— WorkDate)
/// intersection. This is the raw, per-entry profit ledger.
///
/// Created by <c>ProfitCalculationConsumer</c> from every approved billable timesheet entry.
/// Revenue is Hours Ã— BillableRate (from ProjectResource).
/// Cost is Hours Ã— CostPerHour (from EmployeeCostSnapshot).
///
/// This is NOT the table you query for dashboards â€” use <see cref="ProjectMonthlyMetrics"/>
/// for pre-aggregated reads.
/// </summary>
public sealed class ProjectProfitLedger : ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the project the cost/revenue applies to.</summary>
    public Guid ProjectId { get; private set; }

    /// <summary>Gets the employee who performed the work.</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>Gets the source timesheet for idempotency tracing.</summary>
    public Guid TimesheetId { get; private set; }

    /// <summary>Gets the date the work was performed.</summary>
    public DateOnly WorkDate { get; private set; }

    /// <summary>Gets the billable hours for this entry.</summary>
    public decimal Hours { get; private set; }

    /// <summary>Gets the revenue generated (Hours Ã— BillableRate).</summary>
    public decimal Revenue { get; private set; }

    /// <summary>Gets the allocated employee cost (Hours Ã— CostPerHour).</summary>
    public decimal Cost { get; private set; }

    /// <summary>Gets the profit (Revenue âˆ’ Cost).</summary>
    public decimal Profit => Revenue - Cost;

    /// <summary>Gets the margin percentage (Profit / Revenue Ã— 100). Returns 0 if Revenue is zero.</summary>
    public decimal MarginPercent => Revenue == 0 ? 0 : Math.Round(Profit / Revenue * 100, 2);

    /// <summary>Gets the billable rate used (from ProjectResource).</summary>
    public decimal BillableRate { get; private set; }

    /// <summary>Gets the cost-per-hour used (from EmployeeCostSnapshot).</summary>
    public decimal CostPerHour { get; private set; }

    /// <summary>Gets the currency.</summary>
    public string Currency { get; private set; } = "INR";

    private ProjectProfitLedger() { }

    /// <summary>
    /// Creates a new profit ledger entry from a single billable timesheet entry.
    /// </summary>
    public static ProjectProfitLedger Create(
        Guid timesheetId,
        Guid employeeId,
        Guid projectId,
        DateOnly workDate,
        decimal hours,
        decimal billableRate,
        decimal costPerHour,
        string currency)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hours);
        ArgumentNullException.ThrowIfNull(currency);

        decimal revenue = Math.Round(hours * billableRate, 2);
        decimal cost = Math.Round(hours * costPerHour, 2);

        return new ProjectProfitLedger
        {
            Id = Guid.NewGuid(),
            TimesheetId = timesheetId,
            EmployeeId = employeeId,
            ProjectId = projectId,
            WorkDate = workDate,
            Hours = hours,
            Revenue = revenue,
            Cost = cost,
            BillableRate = billableRate,
            CostPerHour = costPerHour,
            Currency = currency.ToUpperInvariant()
        };
    }
}
