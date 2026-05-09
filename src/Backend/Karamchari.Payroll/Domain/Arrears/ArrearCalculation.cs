using Karamchari.Core.Domain.Primitives;
using Karamchari.Payroll.Domain.Arrears.Events;

namespace Karamchari.Payroll.Domain.Arrears;

/// <summary>
/// Aggregate for retroactive arrear calculations spanning one or more past payroll periods.
/// Immutable once Processed; use a new ArrearCalculation for any subsequent correction.
/// </summary>
public sealed class ArrearCalculation : AggregateRoot<Guid>
{
    private readonly List<ArrearPeriodDiff> _periodDiffs = [];

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public ArrearTriggerType TriggerType { get; private set; }
    public string TriggerReference { get; private set; } = string.Empty;  // e.g., revision ID, correction ID
    public ArrearStatus Status { get; private set; }

    // Effective change period
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly EffectiveTo { get; private set; }

    // Aggregate totals
    public decimal TotalGrossDelta { get; private set; }
    public decimal TotalNetDelta { get; private set; }
    public decimal TotalTdsDelta { get; private set; }

    // Payout info — set when Processed
    public string? PayoutPeriodName { get; private set; }
    public string? ProcessedByRunId { get; private set; }

    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public string InitiatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<ArrearPeriodDiff> PeriodDiffs => _periodDiffs.AsReadOnly();

    private ArrearCalculation() { }

    public static ArrearCalculation Create(
        string tenantId,
        Guid employeeId,
        string employeeName,
        ArrearTriggerType triggerType,
        string triggerReference,
        DateOnly effectiveFrom,
        DateOnly effectiveTo,
        string initiatedBy)
    {
        var calc = new ArrearCalculation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            TriggerType = triggerType,
            TriggerReference = triggerReference,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            Status = ArrearStatus.Pending,
            InitiatedBy = initiatedBy,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        calc.RaiseDomainEvent(new ArrearCalculationCreatedEvent(
            calc.Id, tenantId, employeeId, triggerType, effectiveFrom, effectiveTo));

        return calc;
    }

    public void SetPeriodDiffs(IEnumerable<ArrearPeriodDiff> diffs)
    {
        if (Status != ArrearStatus.Pending)
            throw new InvalidOperationException($"Cannot set diffs on arrear in status {Status}.");

        _periodDiffs.Clear();
        _periodDiffs.AddRange(diffs);
        RecalculateTotals();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SubmitForApproval()
    {
        if (Status != ArrearStatus.Pending)
            throw new InvalidOperationException($"Cannot submit arrear in status {Status}.");

        if (_periodDiffs.Count == 0)
            throw new InvalidOperationException("Cannot submit arrear with no period diffs.");

        Status = ArrearStatus.PendingApproval;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ArrearApprovalRequestedEvent(Id, TenantId, EmployeeId, TotalNetDelta));
    }

    public void Approve(string approvedBy)
    {
        if (Status != ArrearStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve arrear in status {Status}.");

        Status = ArrearStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkProcessed(string payoutPeriodName, string runId)
    {
        if (Status != ArrearStatus.Approved)
            throw new InvalidOperationException($"Cannot process arrear in status {Status}.");

        Status = ArrearStatus.Processed;
        PayoutPeriodName = payoutPeriodName;
        ProcessedByRunId = runId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ArrearProcessedEvent(Id, TenantId, EmployeeId, TotalNetDelta, payoutPeriodName));
    }

    public void Reverse(string reason)
    {
        if (Status != ArrearStatus.Processed)
            throw new InvalidOperationException("Only processed arrears can be reversed.");

        Status = ArrearStatus.Reversed;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ArrearReversedEvent(Id, TenantId, EmployeeId, reason));
    }

    private void RecalculateTotals()
    {
        TotalGrossDelta = _periodDiffs.Sum(d => d.GrossDelta);
        TotalNetDelta = _periodDiffs.Sum(d => d.NetDelta);
        TotalTdsDelta = _periodDiffs.Sum(d => d.TdsDelta);
    }
}
