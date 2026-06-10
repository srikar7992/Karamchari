// -----------------------------------------------------------------------
// <copyright file="TenantAwareSagaState.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Messaging.Sagas.Tenant;

/// <summary>
///     Base class for tenant-aware saga state, providing tenant isolation
///     and correlation tracking for MassTransit sagas.
/// </summary>
public abstract class TenantAwareSagaState
{
    /// <summary>
    ///     Gets or sets the tenant identifier this saga belongs to.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the correlation identifier for message routing.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    ///     Gets or sets the UTC timestamp when this saga was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    ///     Gets or sets the UTC timestamp when this saga was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    ///     Gets or sets the number of retry attempts for this saga.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    ///     Indicates whether this saga has been completed.
    /// </summary>
    public bool IsCompleted => CompletedAt.HasValue;
}
