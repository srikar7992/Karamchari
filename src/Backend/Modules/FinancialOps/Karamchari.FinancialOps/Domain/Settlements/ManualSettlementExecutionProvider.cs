// -----------------------------------------------------------------------
// <copyright file="ManualSettlementExecutionProvider.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.FinancialOps.Contracts.Settlements;
using Microsoft.Extensions.Logging;

namespace Karamchari.FinancialOps.Domain.Settlements;

/// <summary>
/// Execution provider for manual settlements (e.g., cash, bank transfer handled outside the system).
/// Simply logs the intent and transitions the batch status.
/// </summary>
public sealed class ManualSettlementExecutionProvider : ISettlementExecutionProvider
{
    private readonly ILogger<ManualSettlementExecutionProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManualSettlementExecutionProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ManualSettlementExecutionProvider(ILogger<ManualSettlementExecutionProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public SettlementType SupportedType => SettlementType.Manual;

    /// <inheritdoc/>
    public Task ExecuteAsync(ReimbursementSettlementBatch batch, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        _logger.LogInformation(
            "Executing manual settlement batch {BatchId} for tenant {TenantId}. External Ref: {ExternalRef}",
            batch.Id, batch.TenantId, batch.ExternalReference ?? "None");

        // Business logic: Manual settlement is immediately considered "Processing" or "Executed"
        // depending on the workflow. For this provider, we assume the external action is triggered.

        return Task.CompletedTask;
    }
}
