using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Calibration.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record CalibrationSessionFinalized(
    Guid SessionId,
    string TenantId,
    Guid ReviewCycleId,
    int TotalEntries,
    DateTimeOffset OccurredOnUtc) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
}
