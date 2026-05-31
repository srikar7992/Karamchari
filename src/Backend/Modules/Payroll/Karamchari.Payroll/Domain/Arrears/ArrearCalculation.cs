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
    public ArrearTriggerType TriggerType { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TriggerReference { get; private set; } = string.Empty;  // e.g., revision ID, correction ID
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ArrearStatus Status { get; private set; }

    // Effective change period
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly EffectiveFrom { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly EffectiveTo { get; private set; }

    // Aggregate totals
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TotalGrossDelta { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TotalNetDelta { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TotalTdsDelta { get; private set; }

    // Payout info â€” set when Processed
    public string? PayoutPeriodName { get; private set; }
    public string? ProcessedByRunId { get; private set; }

    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string InitiatedBy { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<ArrearPeriodDiff> PeriodDiffs => _periodDiffs.AsReadOnly();

    private ArrearCalculation() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void SetPeriodDiffs(IEnumerable<ArrearPeriodDiff> diffs)
    {
        if (Status != ArrearStatus.Pending)
            throw new InvalidOperationException($"Cannot set diffs on arrear in status {Status}.");

        _periodDiffs.Clear();
        _periodDiffs.AddRange(diffs);
        RecalculateTotals();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void SubmitForApproval()
    {
        if (Status != ArrearStatus.Pending)
            throw new InvalidOperationException($"Cannot submit arrear in status {Status}.");

        if (_periodDiffs.Count == 0)
            throw new InvalidOperationException("Cannot submit arrear with no period diffs.");

        Status = ArrearStatus.Pending;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ArrearApprovalRequestedEvent(Id, TenantId, EmployeeId, TotalNetDelta));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Approve(string approvedBy)
    {
        if (Status != ArrearStatus.Pending)
            throw new InvalidOperationException($"Cannot approve arrear in status {Status}.");

        Status = ArrearStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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
