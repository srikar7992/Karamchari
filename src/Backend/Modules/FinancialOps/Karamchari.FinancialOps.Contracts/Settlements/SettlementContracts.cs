// -----------------------------------------------------------------------
// <copyright file="SettlementContracts.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.FinancialOps.Contracts.Settlements;

/// <summary>
/// Defines the methods by which a reimbursement can be settled.
/// </summary>
public enum SettlementType
{
    /// <summary>Settled through the next payroll run.</summary>
    Payroll = 1,
    /// <summary>Settled via direct bank transfer (off-cycle).</summary>
    DirectBankTransfer = 2,
    /// <summary>Settled via cash payout.</summary>
    Cash = 3,
    /// <summary>Settled as a financial adjustment (no direct payout).</summary>
    Adjustment = 4,
    /// <summary>Settled manually outside the system.</summary>
    Manual = 5
}

/// <summary>
/// Defines the lifecycle states of a settlement batch.
/// </summary>
public enum SettlementBatchStatus
{
    /// <summary>Batch is being prepared.</summary>
    Draft = 1,
    /// <summary>Batch is approved and waiting for execution.</summary>
    PendingExecution = 2,
    /// <summary>Batch is currently being processed by the provider.</summary>
    Executing = 3,
    /// <summary>Batch execution completed successfully.</summary>
    Completed = 4,
    /// <summary>Batch execution failed at the provider level.</summary>
    Failed = 5,
    /// <summary>Some claims in the batch succeeded, others failed.</summary>
    PartiallyCompleted = 6,
    /// <summary>Batch has been reconciled against external provider acknowledgments.</summary>
    Reconciled = 7,
    /// <summary>Batch is finalized and locked against any further changes.</summary>
    Finalized = 8
}

/// <summary>
/// A strongly-typed identifier for a settlement batch.
/// </summary>
/// <param name="Value">The underlying Guid value.</param>
public record SettlementBatchId(Guid Value)
{
    /// <summary>Creates a new unique identifier.</summary>
    public static SettlementBatchId New() => new(Guid.NewGuid());
    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
