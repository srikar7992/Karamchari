namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Represents an employee's request for leave.
/// </summary>
public sealed class LeaveRequest : AggregateRoot<Guid>, ITenantOwned
{
    private LeaveRequest(Guid id, Guid employeeId, Guid policyId, DateOnly startDate, DateOnly endDate, string? reason) : base(id)
    {
        EmployeeId = employeeId;
        PolicyId = policyId;
        StartDate = startDate;
        EndDate = endDate;
        Reason = reason;
        Status = LeaveRequestStatus.Pending;
        RequestedOnUtc = DateTime.UtcNow;
    }

    private LeaveRequest()
    {
        TenantId = string.Empty;
    }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the employee identifier.
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Gets the leave policy identifier.
    /// </summary>
    public Guid PolicyId { get; private set; }

    /// <summary>
    /// Gets the start date of the leave.
    /// </summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>
    /// Gets the end date of the leave.
    /// </summary>
    public DateOnly EndDate { get; private set; }

    /// <summary>
    /// Gets the reason for the leave.
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// Gets the current status of the request.
    /// </summary>
    public LeaveRequestStatus Status { get; private set; }

    /// <summary>
    /// Gets the date and time the request was made.
    /// </summary>
    public DateTime RequestedOnUtc { get; private set; }

    /// <summary>
    /// Creates a new leave request.
    /// </summary>
    /// <param name="employeeId">The employee identifier.</param>
    /// <param name="policyId">The leave policy identifier.</param>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">The end date.</param>
    /// <param name="reason">The reason.</param>
    /// <returns>A new <see cref="LeaveRequest"/> instance.</returns>
    public static LeaveRequest Create(Guid employeeId, Guid policyId, DateOnly startDate, DateOnly endDate, string? reason = null)
    {
        if (endDate < startDate) throw new ArgumentException("End date cannot be before start date.");
        return new LeaveRequest(Guid.NewGuid(), employeeId, policyId, startDate, endDate, reason?.Trim());
    }

    /// <summary>
    /// Approves the leave request.
    /// </summary>
    public void Approve()
    {
        if (Status != LeaveRequestStatus.Pending) throw new InvalidOperationException("Only pending requests can be approved.");
        Status = LeaveRequestStatus.Approved;
    }

    /// <summary>
    /// Rejects the leave request.
    /// </summary>
    public void Reject()
    {
        if (Status != LeaveRequestStatus.Pending) throw new InvalidOperationException("Only pending requests can be rejected.");
        Status = LeaveRequestStatus.Rejected;
    }
}
