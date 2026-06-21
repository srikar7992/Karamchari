using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Forecasting.Domain.Events;

public sealed record ScenarioProjectionComputed(
    Guid ScenarioId, string TenantId, string ScenarioName, int MonthsProjected) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record HeadcountVarianceAlert(
    Guid ScenarioId, string TenantId, Guid DepartmentId, int Month, int Variance) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
