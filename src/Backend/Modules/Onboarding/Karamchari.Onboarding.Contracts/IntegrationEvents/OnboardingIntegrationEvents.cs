// -----------------------------------------------------------------------
// <copyright file="OnboardingIntegrationEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Contracts;
using Karamchari.Core.Messaging;
namespace Karamchari.Onboarding.Contracts.IntegrationEvents;

/// <summary>
/// Fired when an onboarding case is created (hire event received or manual initiation).
/// Consumers: Notifications (welcome email), AssetManagement (queue provisioning).
/// </summary>
public sealed record OnboardingCaseCreatedIntegrationEventV1(
    Guid CaseId,
    string TenantId,
    Guid EmployeeId,
    string EmployeeName,
    string CaseType,           // NewHire | Crossboard | Offboard
    DateTimeOffset ExpectedStartDate,
    string? Department,
    string? Role) : IIntegrationEvent;

/// <summary>
/// Fired when all tasks in an onboarding case reach terminal state.
/// Consumers: HR (activate employee), Capability (assign learning path), Notifications.
/// </summary>
public sealed record OnboardingCaseCompletedIntegrationEventV1(
    Guid CaseId,
    string TenantId,
    Guid EmployeeId,
    string CaseType,
    DateTimeOffset CompletedAt) : IIntegrationEvent;

/// <summary>
/// Fired when an employee submits a required document.
/// Consumers: HR (track compliance), Notifications (alert verifier).
/// </summary>
public sealed record OnboardingDocumentSubmittedIntegrationEventV1(
    Guid CaseId,
    Guid DocumentId,
    string TenantId,
    Guid EmployeeId,
    string DocumentType,
    string StorageReference) : IIntegrationEvent;

/// <summary>
/// Fired when offboarding case is completed — employee has exited.
/// Consumers: AssetManagement (mark assets returned), HR (terminate), Payroll (final settlement).
/// </summary>
public sealed record EmployeeOffboardedIntegrationEventV1(
    Guid CaseId,
    string TenantId,
    Guid EmployeeId,
    DateTimeOffset ExitDate,
    string? ExitReason) : IIntegrationEvent;

/// <summary>
/// Published by the EmployeeOnboarding saga when payroll provisioning AND benefits
/// enrollment have both completed for a newly onboarded employee.
/// </summary>
public sealed record EmployeeOnboardingCompletedIntegrationEventV1(
    Guid EmployeeId,
    string TenantId) : IIntegrationEvent;

/// <summary>
/// Published by the EmployeeOnboarding saga when provisioning did NOT complete within
/// the deadline. Signals a half-provisioned employee that needs operator attention —
/// the employee must NOT be treated as fully onboarded.
/// </summary>
public sealed record EmployeeOnboardingIncompleteIntegrationEventV1(
    Guid EmployeeId,
    string TenantId,
    string Reason,
    bool PayrollProvisioned,
    bool BenefitsEnrolled) : IIntegrationEvent;
