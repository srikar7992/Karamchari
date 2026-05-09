using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.SalaryRevisions.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record SalaryRevisionApprovalRequestedEvent(
    Guid RevisionId,
    string TenantId,
    Guid EmployeeId,
    decimal NewCTC,
    DateOnly EffectiveFrom) : IDomainEvent
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
public sealed record SalaryRevisionApprovedEvent(
    Guid RevisionId,
    string TenantId,
    Guid EmployeeId,
    decimal PreviousCTC,
    decimal NewCTC,
    DateOnly EffectiveFrom,
    string ApprovedBy) : IDomainEvent
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
