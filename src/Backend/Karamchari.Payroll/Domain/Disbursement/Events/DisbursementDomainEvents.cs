using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Disbursement.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record DisbursementBatchSubmittedEvent(
    Guid BatchId,
    string TenantId,
    Guid RunId,
    string PeriodName,
    decimal TotalAmount) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record DisbursementBatchCompletedEvent(
    Guid BatchId,
    string TenantId,
    Guid RunId,
    int SuccessCount,
    int FailedCount) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record DisbursementBatchFailedEvent(
    Guid BatchId,
    string TenantId,
    Guid RunId,
    string Reason) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
