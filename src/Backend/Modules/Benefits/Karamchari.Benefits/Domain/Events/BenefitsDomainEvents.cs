// -----------------------------------------------------------------------
// <copyright file="BenefitsDomainEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Domain.Events;

using Karamchari.Core.Domain.Primitives;

/// <summary>
/// Raised when a benefit plan is published by an admin, making it available for employee elections.
/// </summary>
public record BenefitPlanPublishedEvent(
    string TenantId,
    BenefitPlanId PlanId,
    BenefitCategory Category,
    string PlanName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Raised when an employee submits their benefit elections for a plan year.
/// Triggers HR review; elections are not active until the enrollment transitions to Active.
/// </summary>
public record BenefitElectionsSubmittedEvent(
    string TenantId,
    BenefitEnrollmentId EnrollmentId,
    Guid EmployeeId,
    int ElectionCount) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Raised when an enrollment is confirmed active.
/// Triggers the integration event that tells Payroll to create deduction rules.
/// </summary>
public record BenefitEnrollmentActivatedEvent(
    string TenantId,
    BenefitEnrollmentId EnrollmentId,
    Guid EmployeeId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Raised when an employee submits a qualifying life event with supporting documentation.
/// Triggers the Workflow module to start the document verification process.
/// </summary>
public record LifeEventSubmittedEvent(
    string TenantId,
    BenefitEnrollmentId EnrollmentId,
    LifeEventId LifeEventId,
    Guid EmployeeId,
    LifeEventType EventType,
    DateOnly EventDate,
    DateTimeOffset WindowClosesAt,
    string? DocumentStorageReference) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Raised when an election change (triggered by a verified life event) is applied and activated.
/// Payroll must update deduction amounts starting from ChangeEffectiveDate.
/// </summary>
public record BenefitElectionChangedEvent(
    string TenantId,
    BenefitEnrollmentId EnrollmentId,
    Guid EmployeeId,
    LifeEventId LifeEventId,
    DateOnly ChangeEffectiveDate) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Raised when an enrollment window opens and elections are accepted.
/// Notifications should alert eligible employees to complete their elections.
/// </summary>
public record EnrollmentWindowOpenedEvent(
    string TenantId,
    EnrollmentWindowId WindowId,
    EnrollmentWindowType WindowType,
    DateOnly PlanYearStart,
    DateOnly PlanYearEnd,
    DateTimeOffset WindowClosesAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Raised when an enrollment window closes. No further elections are accepted.
/// HR reporting should flag any employees who did not submit elections.
/// </summary>
public record EnrollmentWindowClosedEvent(
    string TenantId,
    EnrollmentWindowId WindowId,
    EnrollmentWindowType WindowType,
    DateOnly PlanYearStart) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
