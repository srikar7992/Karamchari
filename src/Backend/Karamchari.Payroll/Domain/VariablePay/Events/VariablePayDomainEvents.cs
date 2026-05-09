using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.VariablePay.Events;

public sealed record VariablePayApprovedEvent(
    Guid AllocationId,
    Guid TenantId,
    Guid EmployeeId,
    VariablePayType Type,
    decimal Amount,
    string ApprovedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record VariablePayPaidOutEvent(
    Guid AllocationId,
    Guid TenantId,
    Guid EmployeeId,
    VariablePayType Type,
    decimal PaidAmount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
