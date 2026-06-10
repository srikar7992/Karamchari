// -----------------------------------------------------------------------
// <copyright file="ProcessedEventLog.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Billing.Domain.Analytics;

/// <summary>
/// Tracks processed event IDs per consumer to ensure exact-once processing.
/// </summary>
public sealed class ProcessedEventLog
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ConsumerName { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;
}
