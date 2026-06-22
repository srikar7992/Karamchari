using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Analytics.Domain.Events;

public sealed record AnalyticsSnapshotCompleted(
    DateOnly SnapshotDate, int RowsWritten, string TenantId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
