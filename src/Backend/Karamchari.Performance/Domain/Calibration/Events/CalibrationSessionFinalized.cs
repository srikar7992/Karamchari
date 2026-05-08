using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Calibration.Events;

public sealed record CalibrationSessionFinalized(
    Guid SessionId,
    string TenantId,
    Guid ReviewCycleId,
    int TotalEntries,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
