using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Payroll.Domain.PayPeriods.Events;

namespace Karamchari.Payroll.Domain.PayPeriods;

/// <summary>
/// Defines a payroll window (e.g., 1 Jan - 31 Jan Monthly).
/// Pay periods for the same tenant must not overlap.
/// </summary>
public sealed class PayPeriod : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public PayrollFrequency Frequency { get; private set; }
    public PayPeriodStatus Status { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset? LockedAt { get; private set; }
    public string? LockedBy { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private PayPeriod() { }

    public static PayPeriod Open(
        string tenantId,
        DateOnly startDate,
        DateOnly endDate,
        PayrollFrequency frequency,
        string name)
    {
        if (endDate <= startDate)
            throw new ArgumentException("EndDate must be after StartDate.");

        var period = new PayPeriod
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StartDate = startDate,
            EndDate = endDate,
            Frequency = frequency,
            Name = name,
            Status = PayPeriodStatus.Open,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        period.RaiseDomainEvent(new PayPeriodOpenedEvent(period.Id, tenantId, startDate, endDate, frequency));
        return period;
    }

    public bool OverlapsWith(DateOnly otherStart, DateOnly otherEnd) =>
        StartDate <= otherEnd && otherStart <= EndDate;

    public void StartCalculating()
    {
        EnsureStatus(PayPeriodStatus.Open);
        Status = PayPeriodStatus.Calculating;
    }

    public void SubmitForApproval()
    {
        EnsureStatus(PayPeriodStatus.Calculating);
        Status = PayPeriodStatus.PendingApproval;
    }

    public void Approve()
    {
        EnsureStatus(PayPeriodStatus.PendingApproval);
        Status = PayPeriodStatus.Approved;
    }

    public void MarkPaid()
    {
        EnsureStatus(PayPeriodStatus.Approved);
        Status = PayPeriodStatus.Paid;
    }

    public void Lock(string lockedBy)
    {
        if (Status == PayPeriodStatus.Locked)
            return;

        EnsureStatus(PayPeriodStatus.Paid);
        Status = PayPeriodStatus.Locked;
        LockedAt = DateTimeOffset.UtcNow;
        LockedBy = lockedBy;
        RaiseDomainEvent(new PayPeriodLockedEvent(Id, TenantId, lockedBy));
    }

    private void EnsureStatus(PayPeriodStatus required)
    {
        if (Status != required)
            throw new InvalidOperationException($"Cannot transition from {Status}; expected {required}.");
    }
}
