using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Capability.Domain.Events;

public sealed record ComplianceTrainingOverdue(
    Guid AssignmentId, string TenantId, Guid EmployeeId, Guid ModuleId, DateOnly DueDate) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record CertificationExpiringSoon(
    Guid CertificationId, string TenantId, Guid EmployeeId, string CertName, DateTimeOffset ExpiresAtUtc) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record LearningPathCompleted(
    Guid EnrollmentId, string TenantId, Guid EmployeeId, Guid PathId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
