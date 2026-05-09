namespace Karamchari.Payroll.Domain.Disbursement;

public sealed class DisbursementEntry
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public string AccountNumber { get; private set; } = string.Empty;
    public string IfscCode { get; private set; } = string.Empty;
    public string BankName { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DisbursementEntryStatus Status { get; private set; }
    public string? BankTransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    // Idempotency: unique per batch+employee prevents duplicate payout
    public string IdempotencyKey { get; private set; } = string.Empty;

    private DisbursementEntry() { }

    public static DisbursementEntry Create(
        Guid batchId,
        Guid employeeId,
        string employeeName,
        string accountNumber,
        string ifscCode,
        string bankName,
        decimal amount)
    {
        return new DisbursementEntry
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            AccountNumber = accountNumber,
            IfscCode = ifscCode,
            BankName = bankName,
            Amount = amount,
            Status = DisbursementEntryStatus.Pending,
            IdempotencyKey = $"{batchId}:{employeeId}"
        };
    }

    public void MarkSuccess(string bankTransactionId)
    {
        Status = DisbursementEntryStatus.Success;
        BankTransactionId = bankTransactionId;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = DisbursementEntryStatus.Failed;
        FailureReason = reason;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Reverse()
    {
        Status = DisbursementEntryStatus.Reversed;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }
}
