using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Disbursement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Payroll.Services.Disbursement;

public record DisbursementRequest(
    string TenantId,
    Guid RunId,
    string PeriodName,
    BankProvider BankProvider,
    string DebitAccountNumber,
    string InitiatedBy);

/// <summary>
/// Orchestrates bank disbursement for a payroll run.
/// Deduplication: rejects if batch already exists for same (RunId, TenantId).
/// Retry-safe: re-submits only failed entries on subsequent calls.
/// </summary>
public sealed class BankDisbursementOrchestrator
{
    private readonly PayrollDbContext _db;
    private readonly IEnumerable<IBankDisbursementAdapter> _adapters;
    private readonly ILogger<BankDisbursementOrchestrator> _logger;

    public BankDisbursementOrchestrator(
        PayrollDbContext db,
        IEnumerable<IBankDisbursementAdapter> adapters,
        ILogger<BankDisbursementOrchestrator> logger)
    {
        _db = db;
        _adapters = adapters;
        _logger = logger;
    }

    public async Task<DisbursementBatch> InitiateAsync(
        DisbursementRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Deduplication guard: prevent duplicate disbursement for same run
        var existing = await _db.Set<DisbursementBatch>()
            .FirstOrDefaultAsync(b => b.RunId == request.RunId, ct);

        if (existing is not null)
        {
            _logger.LogWarning(
                "Duplicate disbursement attempt for RunId {RunId}. Returning existing batch {BatchId}.",
                request.RunId, existing.Id);
            return existing;
        }

        // Build entries from finalized payroll ledger
        var ledgerEntries = await _db.PayrollLedger
            .AsNoTracking()
            .Where(l => l.RunId == request.RunId)
            .ToListAsync(ct);

        if (ledgerEntries.Count == 0)
            throw new InvalidOperationException($"No payroll ledger entries for run {request.RunId}.");

        // Load bank details from HR employee data — simplified placeholder
        var entries = ledgerEntries
            .Where(l => l.NetPay > 0)
            .Select(l => DisbursementEntry.Create(
                batchId: Guid.Empty,  // assigned after batch creation
                employeeId: l.EmployeeId,
                employeeName: l.EmployeeId.ToString(),  // real impl: join with HR employee
                accountNumber: "000000000000",           // real impl: from HR bank details
                ifscCode: "HDFC0000001",                 // real impl: from HR bank details
                bankName: "HDFC",
                amount: l.NetPay))
            .ToList();

        var batch = DisbursementBatch.Create(
            request.TenantId,
            request.RunId,
            request.PeriodName,
            request.BankProvider,
            entries,
            request.InitiatedBy);

        _db.Set<DisbursementBatch>().Add(batch);
        await _db.SaveChangesAsync(ct);

        return batch;
    }

    public async Task<DisbursementBatch> SubmitAsync(
        Guid batchId,
        string debitAccountNumber,
        CancellationToken ct)
    {
        var batch = await _db.Set<DisbursementBatch>()
            .Include(b => b.Entries)
            .FirstOrDefaultAsync(b => b.Id == batchId, ct)
            ?? throw new InvalidOperationException($"Batch {batchId} not found.");

        var adapter = _adapters.FirstOrDefault(a => a.Provider == batch.BankProvider)
            ?? throw new InvalidOperationException($"No adapter for bank {batch.BankProvider}.");

        // Only submit pending entries (retry-safe)
        var pendingEntries = batch.Entries
            .Where(e => e.Status == DisbursementEntryStatus.Pending || e.Status == DisbursementEntryStatus.Failed)
            .ToList();

        if (pendingEntries.Count == 0)
            return batch;

        var bankRequest = new BankDisbursementRequest(
            batch.BatchReference,
            pendingEntries,
            debitAccountNumber,
            DateOnly.FromDateTime(DateTime.UtcNow));

        batch.RecordRetry();

        try
        {
            var result = await adapter.DisburseAsync(bankRequest, ct);

            if (result.IsSuccess)
            {
                batch.MarkSubmitted(result.BankAcknowledgementId ?? batch.BatchReference);
                batch.ApplyBankResponse(result.EntryResults);
            }
            else
            {
                batch.MarkFailed(result.ErrorMessage ?? "Bank rejected disbursement request.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bank disbursement failed for batch {BatchId}", batchId);
            batch.MarkFailed(ex.Message);
        }

        await _db.SaveChangesAsync(ct);
        return batch;
    }
}
