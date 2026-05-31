using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.FinancialOps.Domain.Ledger;

/// <summary>
/// Policy for generating and validating financial execution sequences.
/// Ensures deterministic ordering of operations within a period.
/// </summary>
public sealed class FinancialExecutionSequencePolicy : IFinancialSequencePolicy
{
    private readonly FinancialOpsDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="FinancialExecutionSequencePolicy"/> class.
    /// </summary>
    /// <param name="db">The financial ops DB context.</param>
    public FinancialExecutionSequencePolicy(FinancialOpsDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateSequenceRefAsync(string tenantId, FinancialOperationalPeriodId periodId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(periodId);

        // Simple sequential ordering within a period for this tenant.
        // In a high-concurrency scenario, this would use a DB sequence or a distributed counter.
        // For Karamchari, we use a simple count-based reference for now, but architected for replacement.

        var count = await _db.LedgerEntries
            .Where(x => x.TenantId == tenantId && x.PeriodId == periodId)
            .CountAsync(ct);

        return $"{periodId.Value:N}-{count + 1:D6}";
    }

    /// <inheritdoc/>
    public async Task ValidateSequenceAsync(string tenantId, string sequenceRef, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sequenceRef);

        var exists = await _db.LedgerEntries
            .AnyAsync(x => x.TenantId == tenantId && x.ExecutionSequenceRef == sequenceRef, ct);

        if (exists)
        {
            throw new InvalidOperationException($"Sequence reference {sequenceRef} has already been used for tenant {tenantId}.");
        }
    }
}

/// <summary>
/// Contract for financial execution sequence policy.
/// </summary>
public interface IFinancialSequencePolicy
{
    /// <summary>
    /// Generates a new unique sequence reference for an operation in a period.
    /// </summary>
    Task<string> GenerateSequenceRefAsync(string tenantId, FinancialOperationalPeriodId periodId, CancellationToken ct = default);

    /// <summary>
    /// Validates that a sequence reference is valid and hasn't been used.
    /// </summary>
    Task ValidateSequenceAsync(string tenantId, string sequenceRef, CancellationToken ct = default);
}
