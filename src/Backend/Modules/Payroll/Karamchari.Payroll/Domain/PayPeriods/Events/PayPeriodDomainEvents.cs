using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.PayPeriods.Events;

public sealed record PayPeriodOpenedEvent(Guid PayPeriodId, string TenantId, DateOnly StartDate, DateOnly EndDate, PayrollFrequency Frequency) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record PayPeriodClosedEvent(Guid PayPeriodId, string TenantId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record PayPeriodLockedEvent(Guid PayPeriodId, string TenantId, string LockedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
