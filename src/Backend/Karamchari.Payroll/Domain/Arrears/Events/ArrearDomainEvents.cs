using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Arrears.Events;

public sealed record ArrearCalculationCreatedEvent(
    Guid ArrearId,
    string TenantId,
    Guid EmployeeId,
    ArrearTriggerType TriggerType,
    DateOnly EffectiveFrom,
    DateOnly EffectiveTo) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record ArrearApprovalRequestedEvent(
    Guid ArrearId,
    string TenantId,
    Guid EmployeeId,
    decimal TotalNetDelta) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record ArrearProcessedEvent(
    Guid ArrearId,
    string TenantId,
    Guid EmployeeId,
    decimal TotalNetDelta,
    string PayoutPeriodName) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record ArrearReversedEvent(
    Guid ArrearId,
    string TenantId,
    Guid EmployeeId,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
