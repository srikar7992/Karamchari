using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Benefits.Domain.Events;

public sealed record EnrollmentWindowOpened(
    Guid WindowId, string TenantId, DateOnly StartDate, DateOnly EndDate) : IDomainEvent
{ public Guid EventId { get; init; } = Guid.NewGuid(); public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow; }

public sealed record EmployeeEnrolledInBenefits(
    Guid EnrollmentId, string TenantId, Guid EmployeeId, Guid PlanId) : IDomainEvent
{ public Guid EventId { get; init; } = Guid.NewGuid(); public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow; }

public sealed record BenefitDeductionCalculated(
    Guid RecordId, string TenantId, Guid EmployeeId,
    string PayPeriod, decimal EmployeeDeduction, decimal EmployerContribution) : IDomainEvent
{ public Guid EventId { get; init; } = Guid.NewGuid(); public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow; }
