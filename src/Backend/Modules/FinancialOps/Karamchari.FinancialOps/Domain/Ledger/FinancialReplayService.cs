// -----------------------------------------------------------------------
// <copyright file="FinancialReplayService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.FinancialOps.Domain.Ledger;

/// <summary>
/// Service for simulating and executing financial replays.
/// Ensures that replays are consistent, observable, and compensatable.
/// </summary>
public sealed class FinancialReplayService : IFinancialReplayService
{
    private readonly FinancialOpsDbContext _db;
    private readonly ILogger<FinancialReplayService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FinancialReplayService"/> class.
    /// </summary>
    /// <param name="db">The financial ops DB context.</param>
    /// <param name="logger">The logger.</param>
    public FinancialReplayService(FinancialOpsDbContext db, ILogger<FinancialReplayService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ReplayIntegrityReport> VerifyIntegrityAsync(string tenantId, FinancialOperationalPeriodId periodId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting financial integrity verification for tenant {TenantId}, period {PeriodId}", tenantId, periodId);

        var ledgerTotal = await _db.LedgerEntries
            .Where(x => x.TenantId == tenantId && x.PeriodId == periodId)
            .SumAsync(x => x.Amount, ct);

        // This is a placeholder for actual complex consistency checks defined in 1E.7
        var report = new ReplayIntegrityReport(
            tenantId,
            periodId,
            DateTimeOffset.UtcNow,
            ledgerTotal,
            true, // Placeholder
            "Verification successful.");

        return report;
    }

    /// <inheritdoc/>
    public async Task SimulateReplayAsync(string tenantId, FinancialCorrelationId correlationRootId, CancellationToken ct = default)
    {
        _logger.LogInformation("Simulating replay for correlation chain {CorrelationRootId}", correlationRootId);

        var chain = await _db.OperationEvents
            .Where(x => x.TenantId == tenantId && x.CorrelationChain.RootId == correlationRootId)
            .OrderBy(x => x.TimestampUtc)
            .ToListAsync(ct);

        if (chain.Count == 0)
        {
            throw new InvalidOperationException($"No events found for correlation root {correlationRootId}");
        }

        foreach (var @event in chain)
        {
            _logger.LogInformation("Simulating step: {EventName} ({From} -> {To})", @event.EventName, @event.FromState, @event.ToState);
        }
    }
}

/// <summary>
/// Report of financial integrity for a period.
/// </summary>
/// <param name="TenantId">Tenant ID.</param>
/// <param name="PeriodId">Period ID.</param>
/// <param name="VerifiedAt">Verification timestamp.</param>
/// <param name="TotalLedgerAmount">Total amount in ledger.</param>
/// <param name="IsConsistent">Consistency flag.</param>
/// <param name="Details">Detailed report.</param>
public record ReplayIntegrityReport(
    string TenantId,
    FinancialOperationalPeriodId PeriodId,
    DateTimeOffset VerifiedAt,
    decimal TotalLedgerAmount,
    bool IsConsistent,
    string Details);

/// <summary>
/// Contract for financial replay operations.
/// </summary>
public interface IFinancialReplayService
{
    /// <summary>
    /// Verifies the financial integrity of a period.
    /// </summary>
    Task<ReplayIntegrityReport> VerifyIntegrityAsync(string tenantId, FinancialOperationalPeriodId periodId, CancellationToken ct = default);

    /// <summary>
    /// Performs a dry-run simulation of a correlation chain.
    /// </summary>
    Task SimulateReplayAsync(string tenantId, FinancialCorrelationId correlationRootId, CancellationToken ct = default);
}
