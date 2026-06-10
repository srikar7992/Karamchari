// -----------------------------------------------------------------------
// <copyright file="FinancialOperationEnums.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.FinancialOps.Contracts.Primitives;

/// <summary>
/// Defines the types of workforce financial operations tracked in the ledger.
/// </summary>
public enum FinancialOperationType
{
    /// <summary>Employee expense reimbursement.</summary>
    Reimbursement = 1,
    /// <summary>Deduction for an active loan.</summary>
    LoanDeduction = 2,
    /// <summary>Adjustment against a salary advance.</summary>
    AdvanceAdjustment = 3,
    /// <summary>Recovery of carry-forward balances.</summary>
    CarryForwardRecovery = 4,
    /// <summary>Final settlement of a claim.</summary>
    Settlement = 5,
    /// <summary>Reversal of a previous entry.</summary>
    Reversal = 6,
    /// <summary>Compensation-related adjustment.</summary>
    Compensation = 7,
    /// <summary>Clawback of overpaid amounts.</summary>
    Clawback = 8
}

/// <summary>
/// Defines the operational status of a financial entry.
/// </summary>
public enum FinancialOperationStatus
{
    /// <summary>Operation is pending execution.</summary>
    Pending = 1,
    /// <summary>Operation has been executed.</summary>
    Executed = 2,
    /// <summary>Operation is finalized and locked.</summary>
    Finalized = 3,
    /// <summary>Operation failed execution.</summary>
    Failed = 4,
    /// <summary>Operation was compensated by another entry.</summary>
    Compensated = 5,
    /// <summary>Operation was reversed.</summary>
    Reversed = 6,
    /// <summary>Operation is quarantined for audit.</summary>
    Quarantined = 7
}
