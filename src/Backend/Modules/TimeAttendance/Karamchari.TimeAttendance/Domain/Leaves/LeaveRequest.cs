namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.Leaves.Events;

/// <summary>
/// Employee leave application. Owns the full lifecycle from Draft → Approved/Rejected/Withdrawn/Expired.
/// </summary>
public sealed class LeaveRequest : AggregateRoot<Guid>, ITenantOwned
{
    private LeaveRequest(Guid id, Guid employeeId, Guid policyId, DateOnly startDate,
        DateOnly endDate, double actualDays, LeaveMode mode, string? reason) : base(id)
    {
        EmployeeId = employeeId;
        PolicyId = policyId;
        StartDate = startDate;
        EndDate = endDate;
        ActualDays = actualDays;
        Mode = mode;
        Reason = reason;
        Status = LeaveRequestStatus.Draft;
        RequestedOnUtc = DateTime.UtcNow;
    }

    private LeaveRequest()
    {
        TenantId = string.Empty;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid PolicyId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string? Reason { get; private set; }
    public LeaveRequestStatus Status { get; private set; }
    public double ActualDays { get; private set; }
    public LeaveMode Mode { get; private set; }

    /// <summary>Only relevant when Mode = HalfDay.</summary>
    public HalfDayPeriod? HalfDayPeriod { get; private set; }

    /// <summary>Only relevant when Mode = Hourly.</summary>
    public double? HoursRequested { get; private set; }

    public DateTime RequestedOnUtc { get; private set; }
    public DateTime? SubmittedOnUtc { get; private set; }
    public DateTime? DecidedOnUtc { get; private set; }

    /// <summary>
    /// If leave was converted (e.g. PL → SL mid-leave), tracks original request.
    /// </summary>
    public Guid? ConvertedFromRequestId { get; private set; }

    /// <summary>
    /// Immutable snapshot of policy rules at submission time.
    /// Ensures historical approvals are unaffected by future policy changes.
    /// Stored as owned JSON.
    /// </summary>
    public LeavePolicySnapshot? SubmittedPolicySnapshot { get; private set; }

    public static LeaveRequest Create(Guid employeeId, Guid policyId, DateOnly startDate,
        DateOnly endDate, double actualDays, string? reason = null,
        LeaveMode mode = LeaveMode.FullDay)
    {
        if (endDate < startDate) throw new ArgumentException("End date cannot be before start date.");
        if (actualDays <= 0) throw new ArgumentException("Actual days must be greater than zero.");

        var request = new LeaveRequest(Guid.NewGuid(), employeeId, policyId, startDate,
            endDate, actualDays, mode, reason?.Trim());

        request.RaiseDomainEvent(new LeaveRequestCreated(request.Id, string.Empty,
            employeeId, startDate, endDate, actualDays));

        return request;
    }

    public static LeaveRequest CreateHalfDay(Guid employeeId, Guid policyId, DateOnly date,
        HalfDayPeriod period, string? reason = null)
    {
        var request = new LeaveRequest(Guid.NewGuid(), employeeId, policyId, date, date, 0.5,
            LeaveMode.HalfDay, reason?.Trim())
        {
            HalfDayPeriod = period
        };
        request.RaiseDomainEvent(new LeaveRequestCreated(request.Id, string.Empty,
            employeeId, date, date, 0.5));
        return request;
    }

    public static LeaveRequest CreateHourly(Guid employeeId, Guid policyId, DateOnly date,
        double hours, string? reason = null)
    {
        if (hours <= 0 || hours > 24) throw new ArgumentOutOfRangeException(nameof(hours));
        var days = hours / 8.0;
        var request = new LeaveRequest(Guid.NewGuid(), employeeId, policyId, date, date, days,
            LeaveMode.Hourly, reason?.Trim())
        {
            HoursRequested = hours
        };
        request.RaiseDomainEvent(new LeaveRequestCreated(request.Id, string.Empty,
            employeeId, date, date, days));
        return request;
    }

    /// <summary>
    /// Employee submits draft for manager approval.
    /// Requires a policy snapshot to ensure historical immutability.
    /// </summary>
    public void Submit(LeavePolicySnapshot? snapshot = null)
    {
        if (Status != LeaveRequestStatus.Draft)
            throw new InvalidOperationException($"Cannot submit from status {Status}.");
        SubmittedPolicySnapshot = snapshot;
        Status = LeaveRequestStatus.Submitted;
        SubmittedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new LeaveRequestSubmitted(Id, TenantId, EmployeeId, StartDate, EndDate, ActualDays));
    }

    /// <summary>
    /// Adjusts ActualDays downward when a holiday is retroactively declared within the leave window.
    /// Called by HolidayRecalculationService. Immutable audit trail preserved in balance ledger.
    /// </summary>
    public void AdjustActualDaysForHoliday(DateOnly holidayDate, double daysToRemove)
    {
        if (Status != LeaveRequestStatus.Approved)
            throw new InvalidOperationException("Can only recalculate approved leaves.");
        if (holidayDate < StartDate || holidayDate > EndDate)
            throw new ArgumentOutOfRangeException(nameof(holidayDate), "Holiday date not within leave window.");
        if (ActualDays - daysToRemove < 0)
            throw new InvalidOperationException("Recalculation would result in negative leave days.");

        ActualDays -= daysToRemove;
    }

    /// <summary>Approval workflow picks up the request for multi-level routing.</summary>
    public void StartApproval()
    {
        LeaveStateMachine.Guard(Status, LeaveRequestStatus.PendingApproval);
        Status = LeaveRequestStatus.PendingApproval;
    }

    public void FinalizeApproved()
    {
        LeaveStateMachine.Guard(Status, LeaveRequestStatus.Approved);
        Status = LeaveRequestStatus.Approved;
        DecidedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new LeaveRequestApproved(Id, TenantId, EmployeeId, StartDate, EndDate, ActualDays));
    }

    public void FinalizeRejected(string? reason = null)
    {
        LeaveStateMachine.Guard(Status, LeaveRequestStatus.Rejected);
        Status = LeaveRequestStatus.Rejected;
        DecidedOnUtc = DateTime.UtcNow;
        if (reason is not null) Reason = reason;
        RaiseDomainEvent(new LeaveRequestRejected(Id, TenantId, EmployeeId));
    }

    /// <summary>Employee cancels before or after approval (pre-leave start).</summary>
    public void Cancel()
    {
        LeaveStateMachine.Guard(Status, LeaveRequestStatus.Cancelled);
        var wasApproved = Status == LeaveRequestStatus.Approved;
        Status = LeaveRequestStatus.Cancelled;
        DecidedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new LeaveRequestCancelled(Id, TenantId, EmployeeId, wasApproved));
    }

    /// <summary>Employee withdraws after submission but before approval starts.</summary>
    public void Withdraw()
    {
        LeaveStateMachine.Guard(Status, LeaveRequestStatus.Withdrawn);
        Status = LeaveRequestStatus.Withdrawn;
        DecidedOnUtc = DateTime.UtcNow;
    }

    /// <summary>System expires request that received no action within policy window.</summary>
    public void Expire()
    {
        LeaveStateMachine.Guard(Status, LeaveRequestStatus.Expired);
        Status = LeaveRequestStatus.Expired;
        DecidedOnUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Mid-leave type conversion (e.g. employee falls sick during PL → convert to SL).
    /// Creates a new request linked to original.
    /// </summary>
    public static LeaveRequest ConvertFrom(LeaveRequest original, Guid newPolicyId,
        DateOnly newStart, DateOnly newEnd, double newActualDays)
    {
        var converted = new LeaveRequest(Guid.NewGuid(), original.EmployeeId, newPolicyId,
            newStart, newEnd, newActualDays, original.Mode, original.Reason)
        {
            ConvertedFromRequestId = original.Id
        };
        return converted;
    }
}
