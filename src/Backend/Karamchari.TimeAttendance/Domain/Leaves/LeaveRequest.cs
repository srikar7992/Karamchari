namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Represents an employee's request for leave.
/// Business truth is owned by this aggregate; coordination is managed by Workflow.
/// </summary>
public sealed class LeaveRequest : AggregateRoot<Guid>, ITenantOwned
{
    private LeaveRequest(Guid id, Guid employeeId, Guid policyId, DateOnly startDate, DateOnly endDate, double actualDays, string? reason) : base(id)
    {
        EmployeeId = employeeId;
        PolicyId = policyId;
        StartDate = startDate;
        EndDate = endDate;
        ActualDays = actualDays;
        Reason = reason;
        Status = LeaveRequestStatus.Draft;
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
    /// Gets the current business status of the request.
    /// </summary>
    public LeaveRequestStatus Status { get; private set; }

    /// <summary>
    /// Gets the calculated actual leave days (excluding holidays/weekends).
    /// </summary>
    public double ActualDays { get; private set; }

    /// <summary>
    /// Gets the date and time the request was made.
    /// </summary>
    public DateTime RequestedOnUtc { get; private set; }

    /// <summary>
    /// Creates a new leave request.
    /// </summary>
    public static LeaveRequest Create(Guid employeeId, Guid policyId, DateOnly startDate, DateOnly endDate, double actualDays, string? reason = null)
    {
        if (endDate < startDate) throw new ArgumentException("End date cannot be before start date.");
        if (actualDays <= 0) throw new ArgumentException("Actual days must be greater than zero.");

        var request = new LeaveRequest(Guid.NewGuid(), employeeId, policyId, startDate, endDate, actualDays, reason?.Trim());

        // TenantId is not set in Create because it's ITenantOwned (stamped by interceptor)
        // but for domain events we often need it. The interceptor stamps it on SaveChanges.
        // We'll rely on the consumer selection of WorkflowDefinition to handle TenantId.

        request.RaiseDomainEvent(new Events.LeaveRequestCreated(request.Id, "", employeeId, startDate, endDate, actualDays));

        return request;
    }

    /// <summary>
    /// Finalizes the leave request as approved (Business Truth).
    /// Called by Workflow coordination upon successful completion.
    /// </summary>
    public void FinalizeApproved()
    {
        if (Status != LeaveRequestStatus.Draft) throw new InvalidOperationException("Only draft requests can be approved.");
        Status = LeaveRequestStatus.Approved;
    }

    /// <summary>
    /// Finalizes the leave request as rejected (Business Truth).
    /// </summary>
    public void FinalizeRejected()
    {
        if (Status != LeaveRequestStatus.Draft) throw new InvalidOperationException("Only draft requests can be rejected.");
        Status = LeaveRequestStatus.Rejected;
    }

    /// <summary>
    /// Cancels the leave request.
    /// </summary>
    public void Cancel()
    {
        if (Status == LeaveRequestStatus.Approved) throw new InvalidOperationException("Cannot cancel an already approved leave.");
        Status = LeaveRequestStatus.Cancelled;
    }
}
