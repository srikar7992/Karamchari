namespace Karamchari.Approvals.Domain;

public enum ApprovalRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
    Delegated = 5,
    Escalated = 6,
}
