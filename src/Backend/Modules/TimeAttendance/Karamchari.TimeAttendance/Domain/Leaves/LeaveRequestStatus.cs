namespace Karamchari.TimeAttendance.Domain.Leaves;

/// <summary>
/// Full lifecycle status for a leave request.
/// </summary>
public enum LeaveRequestStatus
{
    /// <summary>Saved but not yet submitted. Employee still editing.</summary>
    Draft = 1,

    /// <summary>Submitted by employee. Awaiting manager action.</summary>
    Submitted = 2,

    /// <summary>In a multi-level approval workflow. At least one approver pending.</summary>
    PendingApproval = 3,

    /// <summary>All approvers approved. Business truth is authoritative.</summary>
    Approved = 4,

    /// <summary>Rejected by manager or HR.</summary>
    Rejected = 5,

    /// <summary>Cancelled by employee before approval.</summary>
    Cancelled = 6,

    /// <summary>Pulled back by employee after submission (before approval started).</summary>
    Withdrawn = 7,

    /// <summary>Auto-expired: submitted but no action taken within policy window.</summary>
    Expired = 8
}
