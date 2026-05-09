using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.SalaryRevisions.Events;

public sealed record SalaryRevisionApprovalRequestedEvent(
    Guid RevisionId,
    Guid TenantId,
    Guid EmployeeId,
    decimal NewCTC,
    DateOnly EffectiveFrom) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record SalaryRevisionApprovedEvent(
    Guid RevisionId,
    Guid TenantId,
    Guid EmployeeId,
    decimal PreviousCTC,
    decimal NewCTC,
    DateOnly EffectiveFrom,
    string ApprovedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
