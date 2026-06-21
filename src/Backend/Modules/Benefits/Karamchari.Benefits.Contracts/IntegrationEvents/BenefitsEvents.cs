// -----------------------------------------------------------------------
// <copyright file="BenefitsEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

// Integration event contracts for the Benefits bounded context.
// Pure data records — no domain logic, no EF entities, no framework dependencies.

using Karamchari.Core.Contracts;
namespace Karamchari.Benefits.Contracts.IntegrationEvents;

/// <summary>
/// A single active benefit election carried within <see cref="BenefitEnrollmentActivatedIntegrationEventV1"/>.
/// Each record represents one coverage category (Medical, Dental, etc.) the employee has elected.
/// </summary>
/// <param name="BenefitPlanId">Plan the employee elected.</param>
/// <param name="Category">Benefit category (Medical, Dental, Vision, etc.).</param>
/// <param name="CoverageTier">Coverage tier chosen (EmployeeOnly, Family, etc.).</param>
/// <param name="EmployeeMonthlyPremium">Employee share of the monthly premium, pre-tax.</param>
/// <param name="EmployerMonthlyPremium">Employer share of the monthly premium.</param>
/// <param name="EffectiveDate">Date from which deductions should begin.</param>
public record ActiveElectionDto(
    Guid BenefitPlanId,
    string Category,
    string CoverageTier,
    decimal EmployeeMonthlyPremium,
    decimal EmployerMonthlyPremium,
    DateOnly EffectiveDate);

/// <summary>
/// Published when a benefit enrollment transitions to Active status.
/// The Payroll module consumes this to create recurring pre-tax deduction rules
/// for each elected plan. The Notifications module consumes this to send the
/// employee a confirmation with their coverage summary.
/// </summary>
/// <param name="EnrollmentId">Stable enrollment identifier — idempotency key for downstream deduction setup.</param>
/// <param name="EmployeeId">Employee whose enrollment activated.</param>
/// <param name="TenantId">Tenant this enrollment belongs to.</param>
/// <param name="PlanYearStartDate">Start of the benefit plan year.</param>
/// <param name="PlanYearEndDate">End of the benefit plan year.</param>
/// <param name="ActiveElections">All elected plans with cost breakdown. Empty = employee waived all coverage.</param>
/// <param name="OccurredAtUtc">Wall-clock time the enrollment was activated.</param>
public record BenefitEnrollmentActivatedIntegrationEventV1(
    Guid EnrollmentId,
    Guid EmployeeId,
    string TenantId,
    DateOnly PlanYearStartDate,
    DateOnly PlanYearEndDate,
    IReadOnlyList<ActiveElectionDto> ActiveElections,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent;

/// <summary>
/// Published when an employee's benefit election changes mid-year due to a qualifying life event
/// (marriage, new child, loss of other coverage, etc.).
/// Payroll must update deduction amounts; previous deductions remain unchanged for closed periods.
/// </summary>
/// <param name="EnrollmentId">Enrollment whose elections changed.</param>
/// <param name="EmployeeId">Affected employee.</param>
/// <param name="TenantId">Tenant this change belongs to.</param>
/// <param name="LifeEventType">Type of qualifying life event that triggered the change window.</param>
/// <param name="ChangeEffectiveDate">Date from which new deduction amounts apply.</param>
/// <param name="UpdatedElections">The full post-change set of active elections (replaces prior deductions).</param>
/// <param name="OccurredAtUtc">Wall-clock time the election change was processed.</param>
public record BenefitElectionChangedIntegrationEventV1(
    Guid EnrollmentId,
    Guid EmployeeId,
    string TenantId,
    string LifeEventType,
    DateOnly ChangeEffectiveDate,
    IReadOnlyList<ActiveElectionDto> UpdatedElections,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent;

/// <summary>
/// Published when an employee submits a life event and attaches supporting documentation.
/// The Workflow module consumes this to start the document verification workflow.
/// Until the life event is verified, the change window is open but elections are not activated.
/// </summary>
/// <param name="EnrollmentId">Enrollment that contains this life event.</param>
/// <param name="LifeEventId">The specific life event record.</param>
/// <param name="EmployeeId">Employee who submitted the life event.</param>
/// <param name="TenantId">Tenant context.</param>
/// <param name="LifeEventType">Type of qualifying life event.</param>
/// <param name="EventDate">Date the qualifying event occurred (e.g., marriage date).</param>
/// <param name="WindowClosesAt">Deadline by which the employee must complete their election changes.</param>
/// <param name="DocumentStorageReference">Storage key for the uploaded proof document.</param>
/// <param name="OccurredAtUtc">Submission timestamp.</param>
public record LifeEventSubmittedIntegrationEventV1(
    Guid EnrollmentId,
    Guid LifeEventId,
    Guid EmployeeId,
    string TenantId,
    string LifeEventType,
    DateOnly EventDate,
    DateTimeOffset WindowClosesAt,
    string? DocumentStorageReference,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent;
