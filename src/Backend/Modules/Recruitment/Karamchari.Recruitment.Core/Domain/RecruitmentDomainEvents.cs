using Karamchari.Core.Domain.Primitives;
using MediatR;

namespace Karamchari.Recruitment.Domain;

public abstract record RecruitmentDomainEvent(Guid EventId, DateTimeOffset OccurredOnUtc) : IDomainEvent, INotification
{
    protected RecruitmentDomainEvent() : this(Guid.NewGuid(), DateTimeOffset.UtcNow) { }
}

public sealed record RequisitionCreatedDomainEvent(RequisitionId RequisitionId) : RecruitmentDomainEvent;
public sealed record RequisitionPublishedDomainEvent(RequisitionId RequisitionId) : RecruitmentDomainEvent;
public sealed record CandidateAppliedDomainEvent(ApplicationId ApplicationId, CandidateId CandidateId, RequisitionId RequisitionId) : RecruitmentDomainEvent;
public sealed record ApplicationAdvancedDomainEvent(ApplicationId ApplicationId, ApplicationStatus NewStatus) : RecruitmentDomainEvent;
public sealed record InterviewScheduledDomainEvent(InterviewId InterviewId, ApplicationId ApplicationId) : RecruitmentDomainEvent;
public sealed record InterviewCompletedDomainEvent(InterviewId InterviewId) : RecruitmentDomainEvent;
public sealed record InterviewFeedbackSubmittedDomainEvent(InterviewFeedbackId FeedbackId, InterviewId InterviewId) : RecruitmentDomainEvent;
public sealed record OfferDraftedDomainEvent(OfferId OfferId, ApplicationId ApplicationId) : RecruitmentDomainEvent;
public sealed record OfferApprovedDomainEvent(OfferId OfferId) : RecruitmentDomainEvent;
public sealed record OfferIssuedDomainEvent(OfferId OfferId) : RecruitmentDomainEvent;
public sealed record OfferAcceptedDomainEvent(OfferId OfferId) : RecruitmentDomainEvent;
public sealed record OfferDeclinedDomainEvent(OfferId OfferId) : RecruitmentDomainEvent;
public sealed record CandidateHiredDomainEvent(ApplicationId ApplicationId, CandidateId CandidateId) : RecruitmentDomainEvent;
