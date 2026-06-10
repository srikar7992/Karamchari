// -----------------------------------------------------------------------
// <copyright file="ReconciliationEnums.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.Reconciliation;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum ReconciliationJobStatus
{
    Running,
    Completed,
    CompletedWithAnomalies,
    Failed
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum AnomalyType
{
    DuplicatePayout,
    MissingDeduction,
    NegativeNetPay,
    TaxMismatch,
    ArrearsMismatch,
    LoanDeductionMismatch,
    FailedDisbursement,
    OrphanedPayrollState,
    ComplianceMismatch,
    GrossVarianceHigh
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum AnomalySeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum AnomalyResolutionStatus
{
    Open,
    Acknowledged,
    Resolved,
    Dismissed
}
