using Karamchari.Core.Domain.Primitives;
using Karamchari.Payroll.Domain.FnF.Events;

namespace Karamchari.Payroll.Domain.FnF;

/// <summary>
/// Aggregate root for Full and Final settlement of an exiting employee.
/// Settlement is append-only: line items added, status advanced via methods.
/// RowVersion provides optimistic concurrency guard against concurrent edits.
/// </summary>
public sealed class FnFSettlement : AggregateRoot<Guid>
{
    private readonly List<FnFLineItem> _lineItems = [];

    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public FnFExitType ExitType { get; private set; }
    public DateOnly LastWorkingDay { get; private set; }
    public DateOnly ExitInitiatedOn { get; private set; }
    public FnFStatus Status { get; private set; }
    public string? LegalHoldReason { get; private set; }

    // Calculated summary — updated by RecalculateTotals()
    public decimal TotalEarnings { get; private set; }
    public decimal TotalDeductions { get; private set; }
    public decimal NetSettlementAmount { get; private set; }

    // Approval / workflow
    public string? InitiatedBy { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public string? DisbursedBy { get; private set; }
    public DateTimeOffset? DisbursedAtUtc { get; private set; }

    // Idempotency
    public byte[] RowVersion { get; private set; } = [];
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<FnFLineItem> LineItems => _lineItems.AsReadOnly();

    private FnFSettlement() { }

    public static FnFSettlement Initiate(
        Guid tenantId,
        Guid employeeId,
        string employeeName,
        FnFExitType exitType,
        DateOnly lastWorkingDay,
        string initiatedBy)
    {
        var settlement = new FnFSettlement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            ExitType = exitType,
            LastWorkingDay = lastWorkingDay,
            ExitInitiatedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = FnFStatus.Draft,
            InitiatedBy = initiatedBy,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        settlement.RaiseDomainEvent(new FnFSettlementInitiatedEvent(
            settlement.Id, tenantId, employeeId, exitType, lastWorkingDay));

        return settlement;
    }

    public void AddLineItem(FnFLineItem item)
    {
        EnsureEditable();
        _lineItems.Add(item);
        RecalculateTotals();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void ReplaceLineItems(IEnumerable<FnFLineItem> items)
    {
        EnsureEditable();
        _lineItems.Clear();
        _lineItems.AddRange(items);
        RecalculateTotals();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SubmitForApproval()
    {
        if (Status != FnFStatus.Draft && Status != FnFStatus.Reopened)
            throw new InvalidOperationException($"Cannot submit FnF in status {Status}.");

        if (_lineItems.Count == 0)
            throw new InvalidOperationException("FnF must have at least one line item before submission.");

        Status = FnFStatus.PendingApproval;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new FnFApprovalRequestedEvent(Id, TenantId, EmployeeId, NetSettlementAmount));
    }

    public void Approve(string approvedBy)
    {
        if (Status != FnFStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve FnF in status {Status}.");

        Status = FnFStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new FnFApprovedEvent(Id, TenantId, EmployeeId, NetSettlementAmount, approvedBy));
    }

    public void MarkDisbursed(string disbursedBy)
    {
        if (Status != FnFStatus.Approved)
            throw new InvalidOperationException($"Cannot mark disbursed in status {Status}.");

        Status = FnFStatus.Disbursed;
        DisbursedBy = disbursedBy;
        DisbursedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new FnFDisbursedEvent(Id, TenantId, EmployeeId, NetSettlementAmount));
    }

    public void PlaceOnLegalHold(string reason)
    {
        if (Status == FnFStatus.Disbursed || Status == FnFStatus.Cancelled)
            throw new InvalidOperationException($"Cannot hold FnF in status {Status}.");

        Status = FnFStatus.OnHold;
        LegalHoldReason = reason;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void ReleaseHold()
    {
        if (Status != FnFStatus.OnHold)
            throw new InvalidOperationException("Settlement is not on hold.");

        Status = FnFStatus.Draft;
        LegalHoldReason = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Reopen(string reason)
    {
        // Allow reopening post-disbursement for corrections
        if (Status == FnFStatus.Cancelled)
            throw new InvalidOperationException("Cancelled settlement cannot be reopened.");

        Status = FnFStatus.Reopened;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new FnFReopenedEvent(Id, TenantId, EmployeeId, reason));
    }

    public void Cancel(string reason)
    {
        if (Status == FnFStatus.Disbursed)
            throw new InvalidOperationException("Disbursed settlement cannot be cancelled. Initiate a correction instead.");

        Status = FnFStatus.Cancelled;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void EnsureEditable()
    {
        if (Status is FnFStatus.Approved or FnFStatus.Disbursed or FnFStatus.Cancelled or FnFStatus.OnHold)
            throw new InvalidOperationException($"FnF in status {Status} cannot be modified.");
    }

    private void RecalculateTotals()
    {
        TotalEarnings = _lineItems.Where(i => !i.IsDeduction).Sum(i => i.Amount);
        TotalDeductions = _lineItems.Where(i => i.IsDeduction).Sum(i => i.Amount);
        NetSettlementAmount = TotalEarnings - TotalDeductions;
    }
}
