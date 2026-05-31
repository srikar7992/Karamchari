using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Promotions.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
}
