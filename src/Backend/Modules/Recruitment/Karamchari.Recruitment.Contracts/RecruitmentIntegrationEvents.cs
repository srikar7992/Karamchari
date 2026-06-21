// -----------------------------------------------------------------------
// <copyright file="RecruitmentIntegrationEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Contracts;
namespace Karamchari.Recruitment.Contracts;

/// <summary>
/// Published when a candidate is officially hired. 
/// Consumed by the HR module to materialize a new Employee record.
/// </summary>
public record CandidateHiredIntegrationEventV1(
    Guid CandidateId,
    Guid ApplicationId,
    Guid RequisitionId,
    string TenantId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    DateTimeOffset HiredAt,
    string HiredBy,
    decimal BaseSalary,
    string Currency) : IIntegrationEvent;

/// <summary>
/// Published when an offer is issued to a candidate.
/// Consumed by the Notification module to send the offer letter.
/// </summary>
public record OfferIssuedIntegrationEventV1(
    Guid OfferId,
    Guid CandidateId,
    string TenantId,
    string CandidateEmail,
    string CandidateName,
    decimal BaseSalary,
    string Currency,
    DateTimeOffset ExpiresAt) : IIntegrationEvent;

/// <summary>
/// Published when a candidate applies for a requisition.
/// </summary>
public record CandidateAppliedIntegrationEventV1(
    Guid ApplicationId,
    Guid CandidateId,
    Guid RequisitionId,
    string TenantId,
    string CandidateName,
    string RequisitionTitle,
    DateTimeOffset AppliedAt) : IIntegrationEvent;

/// <summary>
/// Published when a requisition is published and becomes open for applications.
/// </summary>
public record RequisitionPublishedIntegrationEventV1(
    Guid RequisitionId,
    string TenantId,
    string Title,
    string DepartmentId,
    DateTimeOffset PublishedAt) : IIntegrationEvent;

/// <summary>
/// Published when an interview is scheduled.
/// </summary>
public record InterviewScheduledIntegrationEventV1(
    Guid InterviewId,
    Guid ApplicationId,
    string TenantId,
    string CandidateName,
    DateTimeOffset ScheduledAt,
    int DurationMinutes) : IIntegrationEvent;

/// <summary>
/// Published when an interview is completed.
/// </summary>
public record InterviewCompletedIntegrationEventV1(
    Guid InterviewId,
    Guid ApplicationId,
    string TenantId,
    DateTimeOffset CompletedAt) : IIntegrationEvent;

/// <summary>
/// Published when an offer is accepted by a candidate.
/// </summary>
public record OfferAcceptedIntegrationEventV1(
    Guid OfferId,
    Guid ApplicationId,
    string TenantId,
    DateTimeOffset AcceptedAt) : IIntegrationEvent;
