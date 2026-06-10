// -----------------------------------------------------------------------
// <copyright file="JobRequisition.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Recruitment.Domain;

public sealed class JobRequisition : AggregateRoot<RequisitionId>, ITenantOwned, IAuditable
{
    private JobRequisition() : base(default) { Title = null!; }

    private JobRequisition(RequisitionId id, string title, Guid departmentId, Guid hiringManagerId)
        : base(id)
    {
        Title = title;
        DepartmentId = departmentId;
        HiringManagerId = hiringManagerId;
        Status = RequisitionStatus.Draft;
    }

    public string Title { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Guid HiringManagerId { get; private set; }
    public RequisitionStatus Status { get; private set; }
    public DateTimeOffset? TargetHireDate { get; private set; }

    // ITenantOwned
    public string TenantId { get; private set; } = null!;

    // IAuditable
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset? UpdatedOnUtc { get; private set; }
    public string? UpdatedBy { get; private set; }

    public static JobRequisition Create(string title, Guid departmentId, Guid hiringManagerId)
    {
        var requisition = new JobRequisition(RequisitionId.New(), title, departmentId, hiringManagerId);
        requisition.RaiseDomainEvent(new RequisitionCreatedDomainEvent(requisition.Id));
        return requisition;
    }

    public void SubmitForApproval()
    {
        if (Status != RequisitionStatus.Draft)
            throw new InvalidOperationException("Only draft requisitions can be submitted for approval.");

        Status = RequisitionStatus.PendingApproval;
    }

    public void Approve()
    {
        if (Status != RequisitionStatus.PendingApproval)
            throw new InvalidOperationException("Only requisitions pending approval can be approved.");

        Status = RequisitionStatus.Published;
        RaiseDomainEvent(new RequisitionPublishedDomainEvent(Id));
    }

    public void Close()
    {
        Status = RequisitionStatus.Closed;
    }
}
