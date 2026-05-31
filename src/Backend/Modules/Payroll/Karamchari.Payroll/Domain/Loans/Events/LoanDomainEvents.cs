using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Loans.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record LoanCreatedEvent(
    Guid LoanId,
    string TenantId,
    Guid EmployeeId,
    LoanType Type,
    decimal PrincipalAmount) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record LoanClosedEvent(
    Guid LoanId,
    string TenantId,
    Guid EmployeeId,
    LoanStatus ClosureType) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
