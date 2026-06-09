using System;
using Karamchari.Core.Domain.Primitives;

namespace Karamchari.HR.Domain.Organization.Events;

public sealed record PositionStatusChanged(
    Guid PositionId,
    string TenantId,
    PositionStatus Status) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
