// -----------------------------------------------------------------------
// <copyright file="ISettlementExecutionProvider.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.FinancialOps.Contracts.Settlements;

namespace Karamchari.FinancialOps.Domain.Settlements;

/// <summary>
/// Defines the contract for an internal provider capable of executing a settlement batch.
/// This abstraction is bounded to supported settlement types and does not support dynamic plugins.
/// </summary>
public interface ISettlementExecutionProvider
{
    /// <summary>
    /// Gets the settlement type supported by this provider.
    /// </summary>
    SettlementType SupportedType { get; }

    /// <summary>
    /// Initiates the payout execution for a specific batch.
    /// Implementation must handle its own idempotency via the batch identifier.
    /// </summary>
    /// <param name="batch">The settlement batch to execute.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ExecuteAsync(ReimbursementSettlementBatch batch, CancellationToken ct = default);
}
