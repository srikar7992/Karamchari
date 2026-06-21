using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Governance.Domain.Events;

public sealed record SodConflictDetected(
    Guid EmployeeId, string TenantId, string Role1, string Role2, DateTimeOffset DetectedAt)
    : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record PolicyViolationDetected(
    string PolicyName, string TenantId, string EntityType, string EntityId, string Severity)
    : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
