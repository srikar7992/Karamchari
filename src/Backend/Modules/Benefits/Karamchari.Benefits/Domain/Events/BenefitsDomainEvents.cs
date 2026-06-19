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
    string PlanName) : IDomainEvent;

/// <summary>
/// Raised when an employee submits their benefit elections for a plan year.
/// Triggers HR review; elections are not active until the enrollment transitions to Active.
/// </summary>
public record BenefitElectionsSubmittedEvent(
    string TenantId,
    BenefitEnrollmentId EnrollmentId,
    Guid EmployeeId,
    int ElectionCount) : IDomainEvent;

/// <summary>
/// Raised when an enrollment is confirmed active.
/// Triggers the integration event that tells Payroll to create deduction rules.
/// </summary>
public record BenefitEnrollmentActivatedEvent(
    string TenantId,
    BenefitEnrollmentId EnrollmentId,
    Guid EmployeeId) : IDomainEvent;

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
    string? DocumentStorageReference) : IDomainEvent;

/// <summary>
/// Raised when an election change (triggered by a verified life event) is applied and activated.
/// Payroll must update deduction amounts starting from ChangeEffectiveDate.
/// </summary>
public record BenefitElectionChangedEvent(
    string TenantId,
    BenefitEnrollmentId EnrollmentId,
    Guid EmployeeId,
    LifeEventId LifeEventId,
    DateOnly ChangeEffectiveDate) : IDomainEvent;
