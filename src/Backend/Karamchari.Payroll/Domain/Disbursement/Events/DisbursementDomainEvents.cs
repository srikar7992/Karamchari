using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Disbursement.Events;

public sealed record DisbursementBatchSubmittedEvent(
    Guid BatchId,
    string TenantId,
    Guid RunId,
    string PeriodName,
    decimal TotalAmount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record DisbursementBatchCompletedEvent(
    Guid BatchId,
    string TenantId,
    Guid RunId,
    int SuccessCount,
    int FailedCount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record DisbursementBatchFailedEvent(
    Guid BatchId,
    string TenantId,
    Guid RunId,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
