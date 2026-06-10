// -----------------------------------------------------------------------
// <copyright file="VariablePayIntegrationEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Contracts;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record VariablePayApprovedIntegrationEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid AllocationId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string VariablePayType { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Amount { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string? PayoutPeriodName { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; init; }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record VariablePayPaidOutIntegrationEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid AllocationId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal PaidAmount { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; init; }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record SalaryRevisionApprovedIntegrationEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RevisionId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal PreviousCTC { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal NewCTC { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly EffectiveFrom { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool RequiresArrears { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; init; }
}
