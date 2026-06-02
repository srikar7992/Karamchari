using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Payroll.Domain.SalaryRevisions.Events;

namespace Karamchari.Payroll.Domain.SalaryRevisions;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum RevisionType { AnnualIncrement, Promotion, MarketCorrection, RetentionAdjustment, OffCycle }

/// <summary>
/// Authoritative business status for salary revisions.
/// Coordination states (Pending, Under Review) are managed by Workflow.
/// </summary>
public enum RevisionStatus
{
    /// <summary>Revision is being drafted and is not yet authoritative.</summary>
    Draft = 1,

    /// <summary>Revision has been finalized and is authoritative truth for payroll calculation.</summary>
    Finalized = 2,

    /// <summary>Revision has been applied to payroll and cannot be changed.</summary>
    Applied = 3,

    /// <summary>Revision was cancelled or superseded.</summary>
    Cancelled = 4
}

/// <summary>
/// Aggregate for a salary revision (increment, promotion, off-cycle, etc.).
/// Effective date drives arrear generation if revision falls inside a processed period.
/// </summary>
public sealed class SalaryRevision : AggregateRoot<Guid>, ITenantOwned
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
    public RevisionType Type { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public RevisionStatus Status { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal PreviousCTC { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal NewCTC { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal IncrementAmount => NewCTC - PreviousCTC;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal IncrementPercent => PreviousCTC > 0 ? (IncrementAmount / PreviousCTC) * 100m : 0m;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly EffectiveFrom { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool RequiresArrears { get; private set; }  // true if effective date is in past processed period
    public Guid? GeneratedArrearId { get; private set; }

    public string? Remarks { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string InitiatedBy { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private SalaryRevision() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static SalaryRevision Create(
        string tenantId,
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void SubmitForApproval()
    {
        if (Status != RevisionStatus.Draft)
            throw new InvalidOperationException($"Cannot submit revision in status {Status}.");

        // Business invariant: Status remains Draft until finalized by coordination (Workflow).
        RaiseDomainEvent(new SalaryRevisionApprovalRequestedEvent(Id, TenantId, EmployeeId, NewCTC, EffectiveFrom));
    }

    /// <summary>
    /// Finalizes the salary revision, making it authoritative truth.
    /// Called by coordination logic (Workflow) upon successful approval.
    /// </summary>
    public void Finalize(string approvedBy)
    {
        if (Status != RevisionStatus.Draft)
            throw new InvalidOperationException($"Cannot finalize revision in status {Status}.");

        Status = RevisionStatus.Finalized;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new SalaryRevisionApprovedEvent(Id, TenantId, EmployeeId, PreviousCTC, NewCTC, EffectiveFrom, approvedBy));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkApplied(Guid? arrearId)
    {
        if (Status != RevisionStatus.Finalized)
            throw new InvalidOperationException($"Cannot apply revision in status {Status}.");

        Status = RevisionStatus.Applied;
        GeneratedArrearId = arrearId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Cancel(string reason)
    {
        if (Status is RevisionStatus.Applied)
            throw new InvalidOperationException("Applied revision cannot be cancelled.");

        Status = RevisionStatus.Cancelled;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
