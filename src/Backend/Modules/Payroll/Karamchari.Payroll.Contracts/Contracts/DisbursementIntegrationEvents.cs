// -----------------------------------------------------------------------
// <copyright file="DisbursementIntegrationEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Contracts;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record InitiateDisbursementCommand
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RunId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string PeriodName { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string BankProvider { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string InitiatedBy { get; init; } = string.Empty;
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record DisbursementBatchSubmittedIntegrationEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid BatchId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RunId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string PeriodName { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TotalAmount { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; init; }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record DisbursementBatchCompletedIntegrationEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid BatchId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RunId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int SuccessCount { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int FailedCount { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; init; }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record DisbursementBatchFailedIntegrationEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid BatchId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RunId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Reason { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; init; }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record RetryDisbursementCommand
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid BatchId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
}
