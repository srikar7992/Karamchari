using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Arrears.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record ArrearCalculationCreatedEvent(
    Guid ArrearId,
    string TenantId,
    Guid EmployeeId,
    ArrearTriggerType TriggerType,
    DateOnly EffectiveFrom,
    DateOnly EffectiveTo) : IDomainEvent
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
public sealed record ArrearApprovalRequestedEvent(
    Guid ArrearId,
    string TenantId,
    Guid EmployeeId,
    decimal TotalNetDelta) : IDomainEvent
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
public sealed record ArrearProcessedEvent(
    Guid ArrearId,
    string TenantId,
    Guid EmployeeId,
    decimal TotalNetDelta,
    string PayoutPeriodName) : IDomainEvent
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
public sealed record ArrearReversedEvent(
    Guid ArrearId,
    string TenantId,
    Guid EmployeeId,
    string Reason) : IDomainEvent
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
