// -----------------------------------------------------------------------
// <copyright file="RecruitmentDtos.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Recruitment.Contracts;

/// <summary>
/// Detailed information about a candidate.
/// </summary>
public record CandidateDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber);

/// <summary>
/// Response returned after successfully creating a candidate.
/// </summary>
public record CandidateCreatedDto(Guid Id);

/// <summary>
/// Detailed information about a job requisition.
/// </summary>
public record RequisitionDto(
    Guid Id,
    string Title,
    string DepartmentId,
    Guid HiringManagerId,
    string Status,
    DateTimeOffset? TargetHireDate);

/// <summary>
/// Information about a job application.
/// </summary>
public record ApplicationDto(
    Guid Id,
    Guid CandidateId,
    Guid RequisitionId,
    string Status,
    DateTimeOffset AppliedAt);

/// <summary>
/// Information about an employment offer.
/// </summary>
public record OfferDto(
    Guid Id,
    Guid ApplicationId,
    decimal BaseSalary,
    string Currency,
    string Status,
    DateTimeOffset? IssuedAt,
    DateTimeOffset? ExpiresAt);

// -----------------------------------------------------------------------
// Read-side DTOs (Phase 1A — recruitment read model completion).
// -----------------------------------------------------------------------

/// <summary>List projection of a candidate.</summary>
public record CandidateSummaryDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    int ProfileVersion,
    DateTimeOffset CreatedOnUtc);

/// <summary>Immutable snapshot of a candidate embedded on an application.</summary>
public record CandidateSnapshotDto(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    int ProfileVersion);

/// <summary>List projection of an application.</summary>
public record ApplicationSummaryDto(
    Guid Id,
    Guid CandidateId,
    Guid RequisitionId,
    string Status,
    DateTimeOffset AppliedAt,
    DateTimeOffset? HiredAt,
    string? HiredBy);

/// <summary>A single piece of feedback submitted against an interview.</summary>
public record InterviewFeedbackDto(
    Guid Id,
    Guid InterviewerId,
    int Rating,
    string Comments,
    DateTimeOffset SubmittedAt);

/// <summary>A scheduled interview for an application.</summary>
public record InterviewDto(
    Guid Id,
    Guid ApplicationId,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string Status,
    IReadOnlyList<Guid> InterviewerIds,
    IReadOnlyList<InterviewFeedbackDto> Feedback);

/// <summary>A single auditable lifecycle event for a recruitment entity.</summary>
public record TimelineEntryDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string? OldState,
    string NewState,
    DateTimeOffset Timestamp,
    string UserId);

/// <summary>Candidate detail with related applications, interviews, offers, and timeline.</summary>
public record CandidateDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    int ProfileVersion,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? UpdatedOnUtc,
    IReadOnlyList<ApplicationSummaryDto> Applications,
    IReadOnlyList<InterviewDto> Interviews,
    IReadOnlyList<OfferDto> Offers,
    IReadOnlyList<TimelineEntryDto> Timeline);

/// <summary>Application detail with embedded candidate snapshot, interviews, offers, and timeline.</summary>
public record ApplicationDetailDto(
    Guid Id,
    Guid CandidateId,
    Guid RequisitionId,
    string Status,
    DateTimeOffset AppliedAt,
    DateTimeOffset? HiredAt,
    string? HiredBy,
    CandidateSnapshotDto? Candidate,
    IReadOnlyList<InterviewDto> Interviews,
    IReadOnlyList<OfferDto> Offers,
    IReadOnlyList<TimelineEntryDto> Timeline);

/// <summary>A single card in a pipeline stage.</summary>
public record PipelineCandidateDto(
    Guid ApplicationId,
    Guid CandidateId,
    string CandidateName,
    string Email,
    string? PhoneNumber,
    Guid RequisitionId,
    DateTimeOffset AppliedAt,
    string Stage);

/// <summary>A stage in the recruitment pipeline and the cards within it.</summary>
public record PipelineStageDto(
    string Stage,
    IReadOnlyList<PipelineCandidateDto> Cards);

/// <summary>Grouped projection of all active applications across pipeline stages.</summary>
public record PipelineDto(
    IReadOnlyList<PipelineStageDto> Stages,
    IReadOnlyDictionary<string, int> StageCounts);
