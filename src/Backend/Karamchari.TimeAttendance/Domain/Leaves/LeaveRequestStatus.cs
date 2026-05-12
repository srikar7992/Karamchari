namespace Karamchari.TimeAttendance.Domain.Leaves;

/// <summary>
/// Authoritative business status for leave requests.
/// </summary>
public enum LeaveRequestStatus
{
    /// <summary>The request is being drafted or is awaiting coordination.</summary>
    Draft = 1,

    /// <summary>The request has been approved and is authoritative truth.</summary>
    Approved = 2,

    /// <summary>The request was rejected.</summary>
    Rejected = 3,

    /// <summary>The request was cancelled by the employee.</summary>
    Cancelled = 4
}
