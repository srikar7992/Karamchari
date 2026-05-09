using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Reviews.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
}
