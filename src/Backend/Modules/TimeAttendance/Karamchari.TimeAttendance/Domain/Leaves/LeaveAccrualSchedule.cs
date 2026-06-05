namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Tracks next scheduled accrual run for an employee-policy pair.
/// Supports upfront, monthly, biweekly, tenure-based, and event-based models.
/// </summary>
public sealed class LeaveAccrualSchedule : Entity<Guid>, ITenantOwned
{
    private LeaveAccrualSchedule()
    {
        TenantId = string.Empty;
    }

    private LeaveAccrualSchedule(Guid id, Guid employeeId, Guid policyId,
        AccrualFrequency frequency, DateOnly nextRunDate) : base(id)
    {
        EmployeeId = employeeId;
        PolicyId = policyId;
        Frequency = frequency;
        NextRunDate = nextRunDate;
        IsActive = true;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid PolicyId { get; private set; }
    public AccrualFrequency Frequency { get; private set; }
    public DateOnly NextRunDate { get; private set; }
    public DateOnly? LastRunDate { get; private set; }
    public decimal AccruedYtd { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>Paused when employee is on LOP, suspension, or maternity (policy-configurable).</summary>
    public bool IsPaused { get; private set; }
    public string? PauseReason { get; private set; }

    public static LeaveAccrualSchedule Create(Guid employeeId, Guid policyId,
        AccrualFrequency frequency, DateOnly firstRunDate)
    {
        return new LeaveAccrualSchedule(Guid.NewGuid(), employeeId, policyId, frequency, firstRunDate);
    }

    public void RecordRun(DateOnly runDate, decimal amountAccrued, DateOnly nextRunDate)
    {
        LastRunDate = runDate;
        AccruedYtd += amountAccrued;
        NextRunDate = nextRunDate;
    }

    public void Pause(string reason)
    {
        IsPaused = true;
        PauseReason = reason;
    }

    public void Resume()
    {
        IsPaused = false;
        PauseReason = null;
    }

    public void Deactivate() => IsActive = false;
}
