using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Promotions.Events;

public sealed record PromotionRejected(
    Guid RecommendationId,
    string TenantId,
    Guid EmployeeId,
    PromotionApprovalStage RejectedAtStage,
    string Reason,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
