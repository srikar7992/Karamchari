using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Approvals.Domain;

public sealed record DelegationCreated(
    Guid DelegationId,
    string TenantId,
    Guid DelegatingManagerId,
    Guid DelegateManagerId,
    DateOnly EffectiveFrom,
    DateOnly EffectiveTo) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
