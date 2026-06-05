using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Runs.Events;

public sealed record PayrollRunCreatedEvent(Guid RunId, string TenantId, Guid PayPeriodId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record PayrollCalculatedEvent(Guid RunId, string TenantId, int EmployeeCount, decimal TotalGross) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record PayrollApprovedEvent(Guid RunId, string TenantId, string ApprovedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record PayrollPublishedEvent(Guid RunId, string TenantId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record PayrollLockedEvent(Guid RunId, string TenantId, string LockedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record PayrollRecalculatedEvent(Guid RunId, string TenantId, int Version, Guid ParentRunId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
