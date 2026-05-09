using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Reimbursements.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record ReimbursementSubmittedEvent(
    Guid ClaimId,
    string TenantId,
    Guid EmployeeId,
    ReimbursementCategory Category,
    decimal ClaimedAmount) : IDomainEvent
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
public sealed record ReimbursementApprovedEvent(
    Guid ClaimId,
    string TenantId,
    Guid EmployeeId,
    decimal ApprovedAmount,
    string ApprovedBy) : IDomainEvent
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
public sealed record ReimbursementClawedBackEvent(
    Guid ClaimId,
    string TenantId,
    Guid EmployeeId,
    decimal Amount,
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
