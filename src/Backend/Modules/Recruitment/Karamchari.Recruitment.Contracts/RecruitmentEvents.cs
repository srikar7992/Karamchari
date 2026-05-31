using Karamchari.Core.Messaging;

namespace Karamchari.Recruitment.Contracts;

// The payload for the Requisition Created event
/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record JobRequisitionCreatedPayload(
    Guid RequisitionId,
    string Title,
    string DepartmentId,
    string LocationId,
    int HeadcountTarget);

// The payload for the Application Created event
/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record CandidateAppliedPayload(
    Guid ApplicationId,
    Guid CandidateId,
    Guid RequisitionId,
    string Source);

// The payload for the Hiring Decision Completed event
// Consumed by Karamchari.HR to initiate onboarding
/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record HiringDecisionCompletedPayload(
    Guid CandidateId,
    Guid RequisitionId,
    Guid OfferId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    decimal FinalBaseSalary,
    string Currency);
