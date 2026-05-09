using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Corrections.Events;

public sealed record CorrectionCreatedEvent(
    Guid CorrectionId,
    Guid TenantId,
    Guid EmployeeId,
    CorrectionType Type,
    string AffectedPeriodName) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record CorrectionApprovalRequestedEvent(
    Guid CorrectionId,
    Guid TenantId,
    Guid EmployeeId,
    CorrectionType Type,
    string AffectedPeriodName) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record CorrectionApprovedEvent(
    Guid CorrectionId,
    Guid TenantId,
    Guid EmployeeId,
    CorrectionType Type,
    string AffectedPeriodName,
    string ApprovedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record CorrectionProcessedEvent(
    Guid CorrectionId,
    Guid TenantId,
    Guid EmployeeId,
    decimal DifferentialAmount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
