using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.FnF.Events;

public sealed record FnFSettlementInitiatedEvent(
    Guid SettlementId,
    Guid TenantId,
    Guid EmployeeId,
    FnFExitType ExitType,
    DateOnly LastWorkingDay) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record FnFApprovalRequestedEvent(
    Guid SettlementId,
    Guid TenantId,
    Guid EmployeeId,
    decimal NetAmount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record FnFApprovedEvent(
    Guid SettlementId,
    Guid TenantId,
    Guid EmployeeId,
    decimal NetAmount,
    string ApprovedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record FnFDisbursedEvent(
    Guid SettlementId,
    Guid TenantId,
    Guid EmployeeId,
    decimal Amount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record FnFReopenedEvent(
    Guid SettlementId,
    Guid TenantId,
    Guid EmployeeId,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
