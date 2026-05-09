using Karamchari.Core.Domain.Primitives;
using Karamchari.Payroll.Domain.SalaryRevisions.Events;

namespace Karamchari.Payroll.Domain.SalaryRevisions;

public enum RevisionType { AnnualIncrement, Promotion, MarketCorrection, RetentionAdjustment, OffCycle }
public enum RevisionStatus { Draft, PendingApproval, Approved, Applied, Cancelled }

/// <summary>
/// Aggregate for a salary revision (increment, promotion, off-cycle, etc.).
/// Effective date drives arrear generation if revision falls inside a processed period.
/// </summary>
public sealed class SalaryRevision : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public RevisionType Type { get; private set; }
    public RevisionStatus Status { get; private set; }

    public decimal PreviousCTC { get; private set; }
    public decimal NewCTC { get; private set; }
    public decimal IncrementAmount => NewCTC - PreviousCTC;
    public decimal IncrementPercent => PreviousCTC > 0 ? (IncrementAmount / PreviousCTC) * 100m : 0m;

    public DateOnly EffectiveFrom { get; private set; }
    public bool RequiresArrears { get; private set; }  // true if effective date is in past processed period
    public Guid? GeneratedArrearId { get; private set; }

    public string? Remarks { get; private set; }
    public string InitiatedBy { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private SalaryRevision() { }

    public static SalaryRevision Create(
        Guid tenantId,
        Guid employeeId,
        string employeeName,
        RevisionType type,
        decimal previousCTC,
        decimal newCTC,
        DateOnly effectiveFrom,
        bool requiresArrears,
        string initiatedBy,
        string? remarks = null)
    {
        if (newCTC <= 0)
            throw new ArgumentException("New CTC must be positive.");

        var revision = new SalaryRevision
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            Type = type,
            PreviousCTC = previousCTC,
            NewCTC = newCTC,
            EffectiveFrom = effectiveFrom,
            RequiresArrears = requiresArrears,
            Remarks = remarks,
            InitiatedBy = initiatedBy,
            Status = RevisionStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        return revision;
    }

    public void SubmitForApproval()
    {
        if (Status != RevisionStatus.Draft)
            throw new InvalidOperationException($"Cannot submit revision in status {Status}.");

        Status = RevisionStatus.PendingApproval;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new SalaryRevisionApprovalRequestedEvent(Id, TenantId, EmployeeId, NewCTC, EffectiveFrom));
    }

    public void Approve(string approvedBy)
    {
        if (Status != RevisionStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve revision in status {Status}.");

        Status = RevisionStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new SalaryRevisionApprovedEvent(Id, TenantId, EmployeeId, PreviousCTC, NewCTC, EffectiveFrom, approvedBy));
    }

    public void MarkApplied(Guid? arrearId)
    {
        if (Status != RevisionStatus.Approved)
            throw new InvalidOperationException($"Cannot apply revision in status {Status}.");

        Status = RevisionStatus.Applied;
        GeneratedArrearId = arrearId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status is RevisionStatus.Applied)
            throw new InvalidOperationException("Applied revision cannot be cancelled.");

        Status = RevisionStatus.Cancelled;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
