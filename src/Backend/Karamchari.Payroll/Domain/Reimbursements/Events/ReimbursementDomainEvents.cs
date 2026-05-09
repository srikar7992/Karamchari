using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Reimbursements.Events;

public sealed record ReimbursementSubmittedEvent(
    Guid ClaimId,
    Guid TenantId,
    Guid EmployeeId,
    ReimbursementCategory Category,
    decimal ClaimedAmount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record ReimbursementApprovedEvent(
    Guid ClaimId,
    Guid TenantId,
    Guid EmployeeId,
    decimal ApprovedAmount,
    string ApprovedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record ReimbursementClawedBackEvent(
    Guid ClaimId,
    Guid TenantId,
    Guid EmployeeId,
    decimal Amount,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
