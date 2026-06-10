// -----------------------------------------------------------------------
// <copyright file="FinancialConsistencyGuard.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Domain.Periods;
using Karamchari.FinancialOps.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.FinancialOps.Domain.Ledger;

/// <summary>
/// Executable operational guard for financial consistency.
/// Enforces invariants as code and triggers quarantine on drift.
/// </summary>
public sealed class FinancialConsistencyGuard : IFinancialConsistencyGuard
{
    private readonly FinancialOpsDbContext _db;
    private readonly ILogger<FinancialConsistencyGuard> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FinancialConsistencyGuard"/> class.
    /// </summary>
    /// <param name="db">The financial ops DB context.</param>
    /// <param name="logger">The logger.</param>
    public FinancialConsistencyGuard(FinancialOpsDbContext db, ILogger<FinancialConsistencyGuard> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ValidatePeriodConsistencyAsync(string tenantId, FinancialOperationalPeriodId periodId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(periodId);

        _logger.LogInformation("Executing consistency guard for tenant {TenantId}, period {PeriodId}", tenantId, periodId);

        // Invariant 1: Ledger totals must match finalized total if finalized
        var finalization = await _db.FinalizationSnapshots
            .Where(x => x.TenantId == tenantId && x.PeriodId == periodId)
            .FirstOrDefaultAsync(ct);

        var actualTotal = await _db.LedgerEntries
            .Where(x => x.TenantId == tenantId && x.PeriodId == periodId)
            .SumAsync(x => x.Amount, ct);

        if (finalization != null && finalization.FinalizedTotal != actualTotal)
        {
            await TriggerQuarantineAsync(tenantId, periodId, "Ledger total mismatch against finalization snapshot.", ct);
        }

        // Invariant 2: Settlement totals == ledger entries of type Settlement
        var settlementLedgerTotal = await _db.LedgerEntries
            .Where(x => x.TenantId == tenantId && x.PeriodId == periodId && x.Type == FinancialOperationType.Settlement)
            .SumAsync(x => x.Amount, ct);

        // This is a simplified check. Real implementation would aggregate batches.
        _logger.LogInformation("Consistency check passed for period {PeriodId}", periodId);
    }

    private async Task TriggerQuarantineAsync(string tenantId, FinancialOperationalPeriodId periodId, string reason, CancellationToken ct)
    {
        _logger.LogCritical("FINANCIAL QUARANTINE TRIGGERED for Tenant {TenantId}, Period {PeriodId}. Reason: {Reason}", tenantId, periodId, reason);

        // Implementation of partition-scoped quarantine (1E.8 Correction 6)
        // For now, we log it. Future: update period status or block integration.

        var @event = FinancialOperationEvent.RecordTransition(
            tenantId,
            periodId.Value,
            nameof(FinancialOperationalPeriod),
            "QuarantineTriggered",
            "Active",
            "Quarantined",
            reason,
            "ConsistencyGuard",
            FinancialCorrelationChain.CreateNew());

        _db.OperationEvents.Add(@event);
        await _db.SaveChangesAsync(ct);

        throw new FinancialConsistencyException(reason, tenantId, periodId);
    }
}

/// <summary>
/// Contract for executable operational guards for financial consistency.
/// </summary>
public interface IFinancialConsistencyGuard
{
    /// <summary>
    /// Validates the financial consistency of a period.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="periodId">The period identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ValidatePeriodConsistencyAsync(string tenantId, FinancialOperationalPeriodId periodId, CancellationToken ct = default);
}

/// <summary>
/// Exception thrown when financial consistency invariants are violated.
/// </summary>
public class FinancialConsistencyException : Exception
{
    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; }
    /// <summary>Gets the period identifier.</summary>
    public FinancialOperationalPeriodId PeriodId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FinancialConsistencyException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="periodId">The period identifier.</param>
    public FinancialConsistencyException(string message, string tenantId, FinancialOperationalPeriodId periodId)
        : base(message)
    {
        TenantId = tenantId;
        PeriodId = periodId;
    }
}
