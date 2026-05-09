using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Loans.Events;

public sealed record LoanCreatedEvent(
    Guid LoanId,
    Guid TenantId,
    Guid EmployeeId,
    LoanType Type,
    decimal PrincipalAmount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LoanClosedEvent(
    Guid LoanId,
    Guid TenantId,
    Guid EmployeeId,
    LoanStatus ClosureType) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
