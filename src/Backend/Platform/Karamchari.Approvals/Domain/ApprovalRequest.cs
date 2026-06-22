using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Approvals.Domain;

public sealed class ApprovalRequest : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ApprovalAction> _actions = [];

    private ApprovalRequest() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid SubmittedBy { get; private set; }
    public Guid ApproverId { get; private set; }
    public ApprovalRequestType RequestType { get; private set; }
    public Guid SubjectEntityId { get; private set; }
    public string? SubjectDescription { get; private set; }
    public ApprovalRequestStatus Status { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }
    public DateTimeOffset? ActedAtUtc { get; private set; }
    public IReadOnlyList<ApprovalAction> Actions => _actions.AsReadOnly();

    public static ApprovalRequest Create(
        string tenantId,
        Guid submittedBy,
        Guid approverId,
        ApprovalRequestType requestType,
        Guid subjectEntityId,
        string? subjectDescription = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var request = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SubmittedBy = submittedBy,
            ApproverId = approverId,
            RequestType = requestType,
            SubjectEntityId = subjectEntityId,
            SubjectDescription = subjectDescription,
            Status = ApprovalRequestStatus.Pending,
            SubmittedAtUtc = DateTimeOffset.UtcNow,
        };

        request.RaiseDomainEvent(new ApprovalRequestCreated(
            request.Id,
            tenantId,
            submittedBy,
            requestType,
            subjectEntityId,
            approverId));

        return request;
    }

    public void Approve(Guid actorId, string actorName, string? comment = null)
    {
        EnsureCanAct();
        var action = new ApprovalAction(actorId, actorName, ApprovalRequestStatus.Approved, comment, DateTimeOffset.UtcNow);
        _actions.Add(action);
        Status = ApprovalRequestStatus.Approved;
        ActedAtUtc = action.ActedAtUtc;
        RaiseDomainEvent(new ApprovalRequestActioned(Id, TenantId, ApprovalRequestStatus.Approved, actorId, comment));
    }

    public void Reject(Guid actorId, string actorName, string? comment = null)
    {
        EnsureCanAct();
        var action = new ApprovalAction(actorId, actorName, ApprovalRequestStatus.Rejected, comment, DateTimeOffset.UtcNow);
        _actions.Add(action);
        Status = ApprovalRequestStatus.Rejected;
        ActedAtUtc = action.ActedAtUtc;
        RaiseDomainEvent(new ApprovalRequestActioned(Id, TenantId, ApprovalRequestStatus.Rejected, actorId, comment));
    }

    public void Cancel(Guid cancelledBy)
    {
        if (Status is ApprovalRequestStatus.Approved or ApprovalRequestStatus.Rejected)
            throw new InvalidOperationException("Cannot cancel a finalised approval request.");

        Status = ApprovalRequestStatus.Cancelled;
        RaiseDomainEvent(new ApprovalRequestCancelled(Id, TenantId, cancelledBy));
    }

    private void EnsureCanAct()
    {
        if (Status != ApprovalRequestStatus.Pending)
            throw new InvalidOperationException($"Approval request is already {Status}.");
    }
}
