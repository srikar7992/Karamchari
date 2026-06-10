// -----------------------------------------------------------------------
// <copyright file="ArrearIntegrationEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Contracts;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record ArrearCalculationApprovedIntegrationEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ArrearId { get; init; }
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
    public decimal TotalNetDelta { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ApprovedBy { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TriggerType { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; init; }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record ArrearProcessedIntegrationEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ArrearId { get; init; }
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
    public decimal TotalNetDelta { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string PayoutPeriodName { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; init; }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record TriggerArrearCalculationCommand
{
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
    public string TriggerType { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TriggerReference { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly EffectiveFrom { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly EffectiveTo { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string InitiatedBy { get; init; } = string.Empty;
}
