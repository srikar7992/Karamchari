using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Approvals.Domain;

public sealed record ApprovalRequestCreated(
    Guid RequestId,
    string TenantId,
    Guid SubmittedBy,
    ApprovalRequestType RequestType,
    Guid SubjectEntityId,
    Guid ApproverId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record ApprovalRequestActioned(
    Guid RequestId,
    string TenantId,
    ApprovalRequestStatus Decision,
    Guid ActorId,
    string? Comment) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record ApprovalRequestCancelled(
    Guid RequestId,
    string TenantId,
    Guid CancelledBy) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
