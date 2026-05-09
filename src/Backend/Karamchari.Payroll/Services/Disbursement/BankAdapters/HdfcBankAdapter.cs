using Karamchari.Payroll.Domain.Disbursement;
using Microsoft.Extensions.Logging;

namespace Karamchari.Payroll.Services.Disbursement.BankAdapters;

/// <summary>
/// HDFC bank disbursement adapter.
/// In production: generates HDFC-format salary file, uploads to SFTP/API, parses acknowledgement.
/// Stub implementation for development — returns success for all entries.
/// </summary>
public sealed class HdfcBankAdapter : IBankDisbursementAdapter
{
    private readonly ILogger<HdfcBankAdapter> _logger;

    public HdfcBankAdapter(ILogger<HdfcBankAdapter> logger)
    {
        _logger = logger;
    }

    public BankProvider Provider => BankProvider.HDFC;

    public Task<BankDisbursementResult> DisburseAsync(
        BankDisbursementRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "HDFC disbursement submitted. Batch: {BatchRef}, Entries: {Count}, Total: {Total}",
            request.BatchReference, request.Entries.Count, request.Entries.Sum(e => e.Amount));

        // Stub: mark all as success
        var results = request.Entries.Select(e => new EntryResult(
            e.Id, true, $"HDFC-{Guid.NewGuid():N}", null)).ToList();

        return Task.FromResult(new BankDisbursementResult(
            request.BatchReference,
            true,
            results,
            $"HDFC-ACK-{DateTime.UtcNow:yyyyMMddHHmmss}",
            null));
    }

    public Task<BankDisbursementResult> QueryStatusAsync(
        string batchReference,
        CancellationToken ct)
    {
        // Stub: return completed status
        return Task.FromResult(new BankDisbursementResult(
            batchReference, true, [], batchReference, null));
    }
}
