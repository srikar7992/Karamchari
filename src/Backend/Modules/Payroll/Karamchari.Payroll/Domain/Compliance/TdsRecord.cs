// -----------------------------------------------------------------------
// <copyright file="TdsRecord.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.Compliance;

/// <summary>
/// Domain model for the TDS Quarterly Return (Form 24Q).
/// </summary>
public sealed record TdsRecord
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Pan { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string EmployeeName { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal AnnualIncome { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TotalTax { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TdsDeducted { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal RemainingTax { get; init; }
}
