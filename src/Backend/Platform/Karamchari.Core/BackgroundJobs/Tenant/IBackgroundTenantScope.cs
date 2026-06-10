// -----------------------------------------------------------------------
// <copyright file="IBackgroundTenantScope.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Core.BackgroundJobs.Tenant;

/// <summary>
/// Defines the contract for a background job tenant scope that manages tenant context.
/// </summary>
public interface IBackgroundTenantScope
{
    /// <summary>
    /// Gets the current tenant execution envelope.
    /// </summary>
    /// <returns>The current tenant execution envelope.</returns>
    TenantExecutionEnvelope GetCurrentEnvelope();

    /// <summary>
    /// Asynchronously gets the tenant execution envelope.
    /// </summary>
    /// <param name="ct">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the tenant execution envelope.</returns>
    Task<TenantExecutionEnvelope> GetEnvelopeAsync(CancellationToken ct);

    /// <summary>
    /// Gets the current tenant identifier.
    /// </summary>
    string CurrentTenantId { get; }

    /// <summary>
    /// Gets a value indicating whether the current tenant context is valid.
    /// </summary>
    bool IsValid { get; }
}
