namespace Karamchari.Performance.Domain.Promotions;

public enum PromotionStatus
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    ApprovedByManager = 4,
    ApprovedByHR = 5,
    ApprovedByLeadership = 6,
    Approved = 7,
    Rejected = 8,
    Withdrawn = 9
}
