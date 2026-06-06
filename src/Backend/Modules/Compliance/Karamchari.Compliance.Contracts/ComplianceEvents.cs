using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Compliance.Contracts;

public sealed record RetentionActionScheduledEvent(
    Guid ActionId,
    string TenantId,
    string RecordType,
    Guid RecordId,
    string Action,
    DateTime ScheduledFor) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record RetentionActionExecutedEvent(
    Guid ActionId,
    string TenantId,
    string RecordType,
    Guid RecordId,
    string Action,
    DateTime ExecutedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LegalHoldCreatedEvent(
    Guid HoldId,
    string TenantId,
    string CaseReference,
    string Reason,
    DateTime StartDate) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LegalHoldReleasedEvent(
    Guid HoldId,
    string TenantId,
    string CaseReference,
    DateTime ReleasedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record PolicyViolationDetectedEvent(
    Guid ViolationId,
    string TenantId,
    string PolicyCode,
    Guid EmployeeId,
    string Severity,
    DateTime DetectedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record PolicyViolationResolvedEvent(
    Guid ViolationId,
    string TenantId,
    string PolicyCode,
    Guid EmployeeId,
    string Resolution,
    DateTime ResolvedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record ComplianceScoreChangedEvent(
    Guid ScoreId,
    string TenantId,
    string ScopeKey,
    decimal OldScore,
    decimal NewScore,
    string NewLevel,
    DateTime CalculatedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record AuditPackageGeneratedEvent(
    Guid PackageId,
    string TenantId,
    string RequestedBy,
    string PackageUri,
    DateTime CompletedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
