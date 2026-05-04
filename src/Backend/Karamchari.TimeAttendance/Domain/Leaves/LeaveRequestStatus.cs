namespace Karamchari.TimeAttendance.Domain.Leaves;

/// <summary>
/// Defines the status of a leave request.
/// </summary>
public enum LeaveRequestStatus
{
    /// <summary>The request is waiting for approval.</summary>
    Pending = 1,
    /// <summary>The request has been approved.</summary>
    Approved = 2,
    /// <summary>The request has been rejected.</summary>
    Rejected = 3,
    /// <summary>The request has been cancelled by the employee.</summary>
    Cancelled = 4
}
