using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Reviews.Events;

public sealed record ReviewSubmissionSubmitted(
    Guid SubmissionId,
    string TenantId,
    Guid AssignmentId,
    Guid RevieweeId,
    Guid ReviewerId,
    ReviewerRole ReviewerRole,
    decimal ComputedScore,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
