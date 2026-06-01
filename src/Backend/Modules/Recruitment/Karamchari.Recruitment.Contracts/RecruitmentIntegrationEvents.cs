namespace Karamchari.Recruitment.Contracts;

/// <summary>
/// Published when a candidate is officially hired. 
/// Consumed by the HR module to materialize a new Employee record.
/// </summary>
public record CandidateHiredIntegrationEvent(
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
    string Currency);

/// <summary>
/// Published when an offer is issued to a candidate.
/// Consumed by the Notification module to send the offer letter.
/// </summary>
public record OfferIssuedIntegrationEvent(
    Guid OfferId,
    Guid CandidateId,
    string TenantId,
    string CandidateEmail,
    string CandidateName,
    decimal BaseSalary,
    string Currency,
    DateTimeOffset ExpiresAt);

/// <summary>
/// Published when a candidate applies for a requisition.
/// </summary>
public record CandidateAppliedIntegrationEvent(
    Guid ApplicationId,
    Guid CandidateId,
    Guid RequisitionId,
    string TenantId,
    string CandidateName,
    string RequisitionTitle,
    DateTimeOffset AppliedAt);

/// <summary>
/// Published when a requisition is published and becomes open for applications.
/// </summary>
public record RequisitionPublishedIntegrationEvent(
    Guid RequisitionId,
    string TenantId,
    string Title,
    string DepartmentId,
    DateTimeOffset PublishedAt);

/// <summary>
/// Published when an interview is scheduled.
/// </summary>
public record InterviewScheduledIntegrationEvent(
    Guid InterviewId,
    Guid ApplicationId,
    string TenantId,
    string CandidateName,
    DateTimeOffset ScheduledAt,
    int DurationMinutes);

/// <summary>
/// Published when an interview is completed.
/// </summary>
public record InterviewCompletedIntegrationEvent(
    Guid InterviewId,
    Guid ApplicationId,
    string TenantId,
    DateTimeOffset CompletedAt);

/// <summary>
/// Published when an offer is accepted by a candidate.
/// </summary>
public record OfferAcceptedIntegrationEvent(
    Guid OfferId,
    Guid ApplicationId,
    string TenantId,
    DateTimeOffset AcceptedAt);
