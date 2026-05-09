using Karamchari.Core.Domain.Primitives;
using Karamchari.Payroll.Domain.VariablePay.Events;

namespace Karamchari.Payroll.Domain.VariablePay;

/// <summary>
/// Aggregate for a single variable pay allocation (bonus, incentive, etc.).
/// Clawback window and deferred payout both handled here.
/// </summary>
public sealed class VariablePayAllocation : AggregateRoot<Guid>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string EmployeeName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public VariablePayType Type { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public VariablePayStatus Status { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public TaxTreatment TaxTreatment { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal AllocatedAmount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal ProratedAmount { get; private set; }  // after proration rules applied
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal PaidAmount { get; private set; }

    public string? PerformancePeriod { get; private set; }
    public string? PayoutPeriodName { get; private set; }
    public DateOnly? ScheduledPayoutDate { get; private set; }
    public DateOnly? ActualPayoutDate { get; private set; }

    // Clawback window
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int ClawbackWindowMonths { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsClawedBack { get; private set; }
    public string? ClawbackReason { get; private set; }
    public DateTimeOffset? ClawbackAtUtc { get; private set; }

    // Deferred payout
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsDeferred { get; private set; }
    public DateOnly? DeferredUntil { get; private set; }

    // Post-exit handling â€” was employee active at payout?
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool EmployeeExitedBeforePayout { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string AllocatedBy { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private VariablePayAllocation() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void ApplyProration(decimal proratedAmount)
    {
        if (proratedAmount < 0 || proratedAmount > AllocatedAmount)
            throw new ArgumentOutOfRangeException(nameof(proratedAmount));

        ProratedAmount = proratedAmount;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void SubmitForApproval()
    {
        if (Status != VariablePayStatus.Draft)
            throw new InvalidOperationException($"Cannot submit variable pay in status {Status}.");

        Status = VariablePayStatus.PendingApproval;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Approve(string approvedBy)
    {
        if (Status != VariablePayStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve variable pay in status {Status}.");

        Status = VariablePayStatus.Approved;
        ApprovedBy = approvedBy;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new VariablePayApprovedEvent(Id, TenantId, EmployeeId, Type, ProratedAmount, approvedBy));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Defer(DateOnly deferredUntil)
    {
        if (Status != VariablePayStatus.Approved)
            throw new InvalidOperationException($"Cannot defer variable pay in status {Status}.");

        IsDeferred = true;
        DeferredUntil = deferredUntil;
        Status = VariablePayStatus.Deferred;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Schedule(string payoutPeriodName)
    {
        if (Status is not (VariablePayStatus.Approved or VariablePayStatus.Deferred))
            throw new InvalidOperationException($"Cannot schedule variable pay in status {Status}.");

        PayoutPeriodName = payoutPeriodName;
        Status = VariablePayStatus.Scheduled;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void FlagExitBeforePayout()
    {
        EmployeeExitedBeforePayout = true;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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
