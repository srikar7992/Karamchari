using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Recruitment.Domain.Primitives;

namespace Karamchari.Recruitment.Domain.Requisitions;

/// <summary>
/// Aggregate root for a Job Requisition.
/// Orchestrates the hiring demand, budget approvals, and headcount planning.
/// </summary>
public sealed class JobRequisition : AggregateRoot<Guid>, ITenantOwned
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Title { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string DepartmentId { get; private set; } = string.Empty; // Cross-module reference
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string LocationId { get; private set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public RequisitionStatus Status { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public HiringPriority Priority { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int HeadcountTarget { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CompensationRange Budget { get; private set; } = null!;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsConfidential { get; private set; }
    /// <inheritdoc/>
    public string? HiringManagerId { get; private set; }
    /// <inheritdoc/>
    public WorkforceDemandSignal? DemandSignal { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
    /// <inheritdoc/>
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    /// <inheritdoc/>
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private JobRequisition() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static JobRequisition Draft(
        string tenantId,
        string title,
        string departmentId,
        string locationId,
        int headcountTarget,
        CompensationRange budget,
        HiringPriority priority,
        bool isConfidential,
        string? hiringManagerId)
    {
        if (headcountTarget <= 0)
            throw new ArgumentException("Headcount target must be greater than zero.");

        var req = new JobRequisition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            DepartmentId = departmentId,
            LocationId = locationId,
            HeadcountTarget = headcountTarget,
            Budget = budget,
            Priority = priority,
            IsConfidential = isConfidential,
            HiringManagerId = hiringManagerId,
            Status = RequisitionStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        return req;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void RequestApproval()
    {
        if (Status != RequisitionStatus.Draft && Status != RequisitionStatus.OnHold)
            throw new InvalidOperationException($"Cannot request approval from state {Status}");

        Status = RequisitionStatus.PendingApproval;
        // Logic to initiate Approval Workflow goes here
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Open(string approvedBy)
    {
        if (Status != RequisitionStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot open requisition from state {Status}");

        Status = RequisitionStatus.Open;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        // Raise RequisitionOpenedEvent
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void PutOnHold()
    {
        if (Status is RequisitionStatus.Filled or RequisitionStatus.Cancelled)
            throw new InvalidOperationException("Cannot hold a closed requisition.");

        Status = RequisitionStatus.OnHold;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void AttachDemandSignal(WorkforceDemandSource source, WorkforceDemandLevel level, string justification, string? sourceReferenceId = null)
    {
        if (Status != RequisitionStatus.Draft && Status != RequisitionStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot attach demand signal to requisition in state {Status}");

        DemandSignal = WorkforceDemandSignal.Create(Id, source, level, justification, sourceReferenceId);
    }
}
