using Karamchari.Core.Domain.Primitives;
using Karamchari.Payroll.Domain.VariablePay.Events;

namespace Karamchari.Payroll.Domain.VariablePay;

/// <summary>
/// Aggregate for a single variable pay allocation (bonus, incentive, etc.).
/// Clawback window and deferred payout both handled here.
/// </summary>
public sealed class VariablePayAllocation : AggregateRoot<Guid>
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public VariablePayType Type { get; private set; }
    public VariablePayStatus Status { get; private set; }
    public TaxTreatment TaxTreatment { get; private set; }

    public decimal AllocatedAmount { get; private set; }
    public decimal ProratedAmount { get; private set; }  // after proration rules applied
    public decimal PaidAmount { get; private set; }

    public string? PerformancePeriod { get; private set; }
    public string? PayoutPeriodName { get; private set; }
    public DateOnly? ScheduledPayoutDate { get; private set; }
    public DateOnly? ActualPayoutDate { get; private set; }

    // Clawback window
    public int ClawbackWindowMonths { get; private set; }
    public bool IsClawedBack { get; private set; }
    public string? ClawbackReason { get; private set; }
    public DateTimeOffset? ClawbackAtUtc { get; private set; }

    // Deferred payout
    public bool IsDeferred { get; private set; }
    public DateOnly? DeferredUntil { get; private set; }

    // Post-exit handling — was employee active at payout?
    public bool EmployeeExitedBeforePayout { get; private set; }

    public string AllocatedBy { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private VariablePayAllocation() { }

    public static VariablePayAllocation Allocate(
        string tenantId,
        Guid employeeId,
        string employeeName,
        VariablePayType type,
        decimal allocatedAmount,
        TaxTreatment taxTreatment,
        string? performancePeriod,
        DateOnly? scheduledPayoutDate,
        int clawbackWindowMonths,
        string allocatedBy)
    {
        var allocation = new VariablePayAllocation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            Type = type,
            AllocatedAmount = allocatedAmount,
            ProratedAmount = allocatedAmount,
            TaxTreatment = taxTreatment,
            PerformancePeriod = performancePeriod,
            ScheduledPayoutDate = scheduledPayoutDate,
            ClawbackWindowMonths = clawbackWindowMonths,
            AllocatedBy = allocatedBy,
            Status = VariablePayStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        return allocation;
    }

    public void ApplyProration(decimal proratedAmount)
    {
        if (proratedAmount < 0 || proratedAmount > AllocatedAmount)
            throw new ArgumentOutOfRangeException(nameof(proratedAmount));

        ProratedAmount = proratedAmount;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SubmitForApproval()
    {
        if (Status != VariablePayStatus.Draft)
            throw new InvalidOperationException($"Cannot submit variable pay in status {Status}.");

        Status = VariablePayStatus.PendingApproval;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Approve(string approvedBy)
    {
        if (Status != VariablePayStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve variable pay in status {Status}.");

        Status = VariablePayStatus.Approved;
        ApprovedBy = approvedBy;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new VariablePayApprovedEvent(Id, TenantId, EmployeeId, Type, ProratedAmount, approvedBy));
    }

    public void Defer(DateOnly deferredUntil)
    {
        if (Status != VariablePayStatus.Approved)
            throw new InvalidOperationException($"Cannot defer variable pay in status {Status}.");

        IsDeferred = true;
        DeferredUntil = deferredUntil;
        Status = VariablePayStatus.Deferred;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Schedule(string payoutPeriodName)
    {
        if (Status is not (VariablePayStatus.Approved or VariablePayStatus.Deferred))
            throw new InvalidOperationException($"Cannot schedule variable pay in status {Status}.");

        PayoutPeriodName = payoutPeriodName;
        Status = VariablePayStatus.Scheduled;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkPaidOut(decimal paidAmount, DateOnly payoutDate)
    {
        if (Status != VariablePayStatus.Scheduled)
            throw new InvalidOperationException($"Cannot mark paid out in status {Status}.");

        PaidAmount = paidAmount;
        ActualPayoutDate = payoutDate;
        Status = VariablePayStatus.PaidOut;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new VariablePayPaidOutEvent(Id, TenantId, EmployeeId, Type, paidAmount));
    }

    public void FlagExitBeforePayout()
    {
        EmployeeExitedBeforePayout = true;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Clawback(string reason)
    {
        if (Status != VariablePayStatus.PaidOut)
            throw new InvalidOperationException("Only paid-out variable pay can be clawed back.");

        IsClawedBack = true;
        ClawbackReason = reason;
        ClawbackAtUtc = DateTimeOffset.UtcNow;
        Status = VariablePayStatus.Clawback;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
