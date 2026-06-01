using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Recruitment.Domain;

public readonly record struct RequisitionId(Guid Value)
{
    public static RequisitionId New() => new(Guid.NewGuid());
}

public readonly record struct CandidateId(Guid Value)
{
    public static CandidateId New() => new(Guid.NewGuid());
}

public readonly record struct ApplicationId(Guid Value)
{
    public static ApplicationId New() => new(Guid.NewGuid());
}

public readonly record struct InterviewId(Guid Value)
{
    public static InterviewId New() => new(Guid.NewGuid());
}

public readonly record struct InterviewFeedbackId(Guid Value)
{
    public static InterviewFeedbackId New() => new(Guid.NewGuid());
}

public readonly record struct OfferId(Guid Value)
{
    public static OfferId New() => new(Guid.NewGuid());
}

public enum RequisitionStatus
{
    Draft = 1,
    PendingApproval = 2,
    Published = 3,
    OnHold = 4,
    Closed = 5,
    Filled = 6
}

public enum ApplicationStatus
{
    New = 1,
    Screening = 2,
    Interviewing = 3,
    Offered = 4,
    Hired = 5,
    Rejected = 6,
    Withdrawn = 7
}

public enum InterviewStatus
{
    Scheduled = 1,
    Completed = 2,
    Cancelled = 3,
    NoShow = 4
}

public enum OfferStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Issued = 4,
    Accepted = 5,
    Declined = 6,
    Expired = 7,
    Rescinded = 8
}
