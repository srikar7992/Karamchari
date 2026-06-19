// -----------------------------------------------------------------------
// <copyright file="BenefitsQueryDtos.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

// Read model DTOs for the Benefits Administration module.
// These travel from module services → BFF endpoints → API clients.
// They are projection-only records — no domain logic, no EF entities.

namespace Karamchari.Benefits.Contracts.Dtos;

public record BenefitPlanTierDto(
    string Tier,
    decimal EmployeeMonthlyPremium,
    decimal EmployerMonthlyPremium,
    decimal TotalMonthlyPremium);

public record BenefitPlanDto(
    Guid PlanId,
    string PlanName,
    string? Description,
    string Category,
    string Status,
    string ProviderName,
    string? ExternalPlanCode,
    DateOnly PlanYearStart,
    DateOnly PlanYearEnd,
    int WaitingPeriodDays,
    IReadOnlyList<BenefitPlanTierDto> Tiers);

public record BenefitElectionDto(
    Guid ElectionId,
    Guid PlanId,
    string Category,
    string CoverageTier,
    decimal EmployeeMonthlyPremium,
    decimal EmployerMonthlyPremium,
    decimal TotalMonthlyPremium,
    DateOnly EffectiveDate);

public record BenefitDependentDto(
    Guid DependentId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Relationship,
    bool EnrolledMedical,
    bool EnrolledDental,
    bool EnrolledVision);

public record LifeEventDto(
    Guid LifeEventId,
    string EventType,
    DateOnly EventDate,
    string Status,
    DateTimeOffset WindowClosesAt,
    bool IsWindowOpen,
    string? DocumentStorageReference,
    string? RejectionReason);

public record EnrollmentDto(
    Guid EnrollmentId,
    Guid EmployeeId,
    string EmployeeName,
    string Status,
    DateOnly PlanYearStart,
    DateOnly PlanYearEnd,
    DateOnly HireDate,
    IReadOnlyList<BenefitElectionDto> ActiveElections,
    IReadOnlyList<BenefitDependentDto> ActiveDependents,
    IReadOnlyList<LifeEventDto> LifeEvents,
    decimal TotalEmployeeMonthlyPremium,
    decimal TotalEmployerMonthlyPremium);

public record EnrollmentSummaryDto(
    Guid EnrollmentId,
    Guid EmployeeId,
    string EmployeeName,
    string Status,
    DateOnly PlanYearStart,
    int ActiveElectionCount,
    decimal TotalEmployeeMonthlyPremium);
