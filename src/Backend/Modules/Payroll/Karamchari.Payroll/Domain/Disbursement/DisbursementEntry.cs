namespace Karamchari.Payroll.Domain.Disbursement;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class DisbursementEntry
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid Id { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string EmployeeName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string AccountNumber { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string IfscCode { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string BankName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Amount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DisbursementEntryStatus Status { get; private set; }
    public string? BankTransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    // Idempotency: unique per batch+employee prevents duplicate payout
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string IdempotencyKey { get; private set; } = string.Empty;

    private DisbursementEntry() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkSuccess(string bankTransactionId)
    {
        Status = DisbursementEntryStatus.Success;
        BankTransactionId = bankTransactionId;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkFailed(string reason)
    {
        Status = DisbursementEntryStatus.Failed;
        FailureReason = reason;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Reverse()
    {
        Status = DisbursementEntryStatus.Reversed;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }
}
