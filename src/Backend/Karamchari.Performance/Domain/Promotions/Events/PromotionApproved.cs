using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Promotions.Events;

public sealed record PromotionApproved(
    Guid RecommendationId,
    string TenantId,
    Guid EmployeeId,
    string PreviousGrade,
    string NewGrade,
    string PreviousTitle,
    string NewTitle,
    DateOnly EffectiveDate,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
