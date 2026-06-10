// -----------------------------------------------------------------------
// <copyright file="VariablePayEnums.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.VariablePay;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum VariablePayType
{
    PerformanceBonus,
    RetentionBonus,
    SalesIncentive,
    SpotBonus,
    JoiningBonus,
    QuarterlyIncentive,
    AnnualBonus,
    ProjectBonus,
    ReferralBonus
}

/// <summary>
/// Authoritative business status for variable pay allocations.
/// Coordination states (Pending Approval) are managed by Workflow.
/// </summary>
public enum VariablePayStatus
{
    /// <summary>Allocation is being drafted.</summary>
    Draft = 1,

    /// <summary>Allocation has been approved and is authoritative truth.</summary>
    Approved = 2,

    /// <summary>Allocation has been scheduled for a specific payroll period.</summary>
    Scheduled = 3,

    /// <summary>Allocation has been paid out.</summary>
    PaidOut = 4,

    /// <summary>Allocation has been clawed back.</summary>
    Clawback = 5,

    /// <summary>Allocation was cancelled or rejected.</summary>
    Cancelled = 6,

    /// <summary>Allocation is deferred to a future date.</summary>
    Deferred = 7
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum TaxTreatment
{
    /// <summary>Spread across months for TDS calculation.</summary>
    Spread,
    /// <summary>Taxed fully in payout month.</summary>
    LumpSum
}
