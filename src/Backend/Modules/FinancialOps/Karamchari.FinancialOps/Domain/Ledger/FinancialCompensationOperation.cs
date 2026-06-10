// -----------------------------------------------------------------------
// <copyright file="FinancialCompensationOperation.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.FinancialOps.Contracts.Primitives;

namespace Karamchari.FinancialOps.Domain.Ledger;

/// <summary>
/// Represents a financial compensation operation.
/// Used to correct errors or reconcile discrepancies without mutating history.
/// </summary>
public sealed class FinancialCompensationOperation : AggregateRoot<Guid>, ITenantOwned
{
    private FinancialCompensationOperation(
        Guid id,
        string tenantId,
        WorkforceFinancialEntryId targetEntryId,
        decimal amount,
        string reason,
        FinancialCorrelationChain correlationChain)
        : base(id)
    {
        TenantId = tenantId;
        TargetEntryId = targetEntryId;
        Amount = amount;
        Reason = reason;
        CorrelationChain = correlationChain;
        CreatedAt = DateTimeOffset.UtcNow;
        Status = CompensationStatus.Pending;
    }

    private FinancialCompensationOperation()
    {
        // EF Core
        TenantId = string.Empty;
        TargetEntryId = null!;
        Reason = string.Empty;
        CorrelationChain = null!;
    }

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; }

    /// <summary>Gets the identifier of the entry being compensated.</summary>
    public WorkforceFinancialEntryId TargetEntryId { get; private set; }

    /// <summary>Gets the compensation amount.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Gets the reason for compensation.</summary>
    public string Reason { get; private set; }

    /// <summary>Gets the status of the compensation operation.</summary>
    public CompensationStatus Status { get; private set; }

    /// <summary>Gets the correlation chain.</summary>
    public FinancialCorrelationChain CorrelationChain { get; private set; }

    /// <summary>Gets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a new compensation operation.</summary>
    public static FinancialCompensationOperation Create(
        string tenantId,
        WorkforceFinancialEntryId targetEntryId,
        decimal amount,
        string reason,
        FinancialCorrelationChain correlationChain)
    {
        return new FinancialCompensationOperation(
            Guid.NewGuid(),
            tenantId,
            targetEntryId,
            amount,
            reason,
            correlationChain);
    }

    /// <summary>Marks the compensation as executed.</summary>
    public void Execute()
    {
        Status = CompensationStatus.Executed;
    }
}

/// <summary>Defines the status of a compensation operation.</summary>
public enum CompensationStatus
{
    /// <summary>Compensation is pending execution.</summary>
    Pending = 1,
    /// <summary>Compensation has been executed.</summary>
    Executed = 2,
    /// <summary>Compensation failed.</summary>
    Failed = 3,
    /// <summary>Compensation was cancelled.</summary>
    Cancelled = 4
}
