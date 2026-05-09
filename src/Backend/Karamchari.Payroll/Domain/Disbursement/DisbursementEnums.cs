namespace Karamchari.Payroll.Domain.Disbursement;

public enum DisbursementBatchStatus
{
    Pending,
    FileGenerated,
    Submitted,
    PartiallyProcessed,
    Completed,
    Failed,
    Reversed
}

public enum DisbursementEntryStatus
{
    Pending,
    Success,
    Failed,
    Reversed,
    Duplicate  // prevented — idempotency guard
}

public enum BankProvider
{
    HDFC,
    ICICI,
    SBI,
    Axis,
    Kotak,
    Yes,
    Generic
}
