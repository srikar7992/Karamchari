using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Corrections.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record CorrectionCreatedEvent(
    Guid CorrectionId,
    string TenantId,
    Guid EmployeeId,
    CorrectionType Type,
    string AffectedPeriodName) : IDomainEvent
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
public sealed record CorrectionApprovalRequestedEvent(
    Guid CorrectionId,
    string TenantId,
    Guid EmployeeId,
    CorrectionType Type,
    string AffectedPeriodName) : IDomainEvent
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
public sealed record CorrectionApprovedEvent(
    Guid CorrectionId,
    string TenantId,
    Guid EmployeeId,
    CorrectionType Type,
    string AffectedPeriodName,
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

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record CorrectionProcessedEvent(
    Guid CorrectionId,
    string TenantId,
    Guid EmployeeId,
    decimal DifferentialAmount) : IDomainEvent
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
