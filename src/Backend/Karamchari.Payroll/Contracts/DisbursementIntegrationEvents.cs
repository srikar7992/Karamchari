namespace Karamchari.Payroll.Contracts;

public sealed record InitiateDisbursementCommand
{
    public string TenantId { get; init; } = string.Empty;
    public Guid RunId { get; init; }
    public string PeriodName { get; init; } = string.Empty;
    public string BankProvider { get; init; } = string.Empty;
    public string InitiatedBy { get; init; } = string.Empty;
}

public sealed record DisbursementBatchSubmittedIntegrationEvent
{
    public Guid BatchId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public Guid RunId { get; init; }
    public string PeriodName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record DisbursementBatchCompletedIntegrationEvent
{
    public Guid BatchId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public Guid RunId { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record DisbursementBatchFailedIntegrationEvent
{
    public Guid BatchId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public Guid RunId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record RetryDisbursementCommand
{
    public Guid BatchId { get; init; }
    public string TenantId { get; init; } = string.Empty;
}
