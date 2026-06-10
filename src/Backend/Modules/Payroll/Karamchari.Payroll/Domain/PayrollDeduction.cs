// -----------------------------------------------------------------------
// <copyright file="PayrollDeduction.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Represents a monetary deduction for an employee in a specific payroll period.
/// </summary>
public sealed class PayrollDeduction : AggregateRoot<Guid>, ITenantOwned
{
    private PayrollDeduction(Guid id, string tenantId, Guid employeeId, string periodName, decimal amount, string reason) : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        PeriodName = periodName;
        Amount = amount;
        Reason = reason;
        IsProcessed = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private PayrollDeduction()
    {
        TenantId = string.Empty;
        PeriodName = string.Empty;
        Reason = string.Empty;
    }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the employee identifier.
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Gets the payroll period name (e.g. "April 2027").
    /// </summary>
    public string PeriodName { get; private set; }

    /// <summary>
    /// Gets the deduction amount.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Gets the reason for the deduction.
    /// </summary>
    public string Reason { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this deduction has been processed in a payroll run.
    /// </summary>
    public bool IsProcessed { get; private set; }

    /// <summary>
    /// Gets the date and time this record was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new payroll deduction.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="employeeId">The employee identifier.</param>
    /// <param name="periodName">The payroll period name.</param>
    /// <param name="amount">The deduction amount.</param>
    /// <param name="reason">The reason for the deduction.</param>
    /// <returns>A new <see cref="PayrollDeduction"/> instance.</returns>
    public static PayrollDeduction Create(string tenantId, Guid employeeId, string periodName, decimal amount, string reason)
    {
        return new PayrollDeduction(Guid.NewGuid(), tenantId, employeeId, periodName, amount, reason);
    }

    /// <summary>
    /// Marks the deduction as processed.
    /// </summary>
    public void MarkAsProcessed()
    {
        IsProcessed = true;
    }
}
