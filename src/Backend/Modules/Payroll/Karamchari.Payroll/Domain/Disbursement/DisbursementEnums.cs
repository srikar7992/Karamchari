// -----------------------------------------------------------------------
// <copyright file="DisbursementEnums.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.Disbursement;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum DisbursementBatchStatus
{
    Pending,
    FileGenerated,
    Submitted,
    PartiallyProcessed,
    Completed,
    Failed,
    Reversed
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum DisbursementEntryStatus
{
    Pending,
    Success,
    Failed,
    Reversed,
    Duplicate  // prevented â€” idempotency guard
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum BankProvider
{
    HDFC,
    ICICI,
    SBI,
    Axis,
    Kotak,
    Yes,
    Generic
}
