// -----------------------------------------------------------------------
// <copyright file="EmployeeAdvance.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.FinancialOps.Contracts.Primitives;

namespace Karamchari.FinancialOps.Domain.Ledger;

/// <summary>
/// Aggregate root for an employee salary advance.
/// Simple short-term advance usually recovered in a single payroll cycle.
/// </summary>
public sealed class EmployeeAdvance : AggregateRoot<Guid>, ITenantOwned
{
    private EmployeeAdvance(
        Guid id,
        string tenantId,
        EmployeeId employeeId,
        decimal amount,
        FinancialOperationalPeriodId periodId,
        FinancialReplayMetadata replayMetadata)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        Amount = amount;
        PeriodId = periodId;
        ReplayMetadata = replayMetadata;
        IsRecovered = false;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private EmployeeAdvance()
    {
        // EF Core
        TenantId = string.Empty;
        EmployeeId = null!;
        PeriodId = null!;
        ReplayMetadata = null!;
    }

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; }

    /// <summary>Gets the employee identifier.</summary>
    public EmployeeId EmployeeId { get; private set; }

    /// <summary>Gets the advance amount.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Gets the period in which the advance was issued.</summary>
    public FinancialOperationalPeriodId PeriodId { get; private set; }

    /// <summary>Gets a value indicating whether the advance has been fully recovered.</summary>
    public bool IsRecovered { get; private set; }

    /// <summary>Gets the timestamp when recovered.</summary>
    public DateTimeOffset? RecoveredAt { get; private set; }

    /// <summary>Gets the replay metadata.</summary>
    public FinancialReplayMetadata ReplayMetadata { get; private set; }

    /// <summary>Gets the timestamp when the advance was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a new employee advance.</summary>
    public static EmployeeAdvance Create(
        string tenantId,
        EmployeeId employeeId,
        decimal amount,
        FinancialOperationalPeriodId periodId,
        FinancialReplayMetadata? replayMetadata = null)
    {
        return new EmployeeAdvance(
            Guid.NewGuid(),
            tenantId,
            employeeId,
            amount,
            periodId,
            replayMetadata ?? FinancialReplayMetadata.Empty);
    }

    /// <summary>Marks the advance as recovered.</summary>
    public void MarkAsRecovered()
    {
        IsRecovered = true;
        RecoveredAt = DateTimeOffset.UtcNow;
    }
}
