namespace Karamchari.Payroll.Domain.Disbursement;

public record BankDisbursementRequest(
    string BatchReference,
    IReadOnlyList<DisbursementEntry> Entries,
    string DebitAccountNumber,
    DateOnly ValueDate);

public record BankDisbursementResult(
    string BatchReference,
    bool IsSuccess,
    IReadOnlyList<EntryResult> EntryResults,
    string? BankAcknowledgementId,
    string? ErrorMessage);

public record EntryResult(
    Guid EntryId,
    bool IsSuccess,
    string? BankTransactionId,
    string? FailureReason);

/// <summary>
/// Bank adapter abstraction. Each bank provides a concrete implementation.
/// Adapter handles file format, encryption, API calls, and acknowledgement parsing.
/// </summary>
public interface IBankDisbursementAdapter
{
    BankProvider Provider { get; }
    Task<BankDisbursementResult> DisburseAsync(BankDisbursementRequest request, CancellationToken ct);
    Task<BankDisbursementResult> QueryStatusAsync(string batchReference, CancellationToken ct);
}
