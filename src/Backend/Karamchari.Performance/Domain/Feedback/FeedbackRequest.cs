using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Feedback;

/// <summary>
/// A request for feedback about a specific subject (employee) from a specific provider.
/// ExpiresOnUtc enforced at application layer — no domain enforcement to allow grace periods.
/// </summary>
public sealed class FeedbackRequest : AggregateRoot<Guid>, ITenantOwned
{
    private FeedbackRequest() { /* EF materialization */ }

    private FeedbackRequest(
        Guid id,
        string tenantId,
        Guid subjectEmployeeId,
        Guid requestedByEmployeeId,
        Guid? feedbackProviderId,
        bool isAnonymous,
        FeedbackRequestType type,
        string? context,
        DateTimeOffset expiresOnUtc) : base(id)
    {
        TenantId = tenantId;
        SubjectEmployeeId = subjectEmployeeId;
        RequestedByEmployeeId = requestedByEmployeeId;
        FeedbackProviderId = feedbackProviderId;
        IsAnonymous = isAnonymous;
        Type = type;
        Context = context;
        ExpiresOnUtc = expiresOnUtc;
        Status = FeedbackRequestStatus.Pending;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid SubjectEmployeeId { get; private set; }
    public Guid RequestedByEmployeeId { get; private set; }

    /// <summary>Null for open/pool requests. Set for directed requests.</summary>
    public Guid? FeedbackProviderId { get; private set; }
    public bool IsAnonymous { get; private set; }
    public FeedbackRequestType Type { get; private set; }
    public string? Context { get; private set; }
    public DateTimeOffset ExpiresOnUtc { get; private set; }
    public FeedbackRequestStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static FeedbackRequest Create(
        string tenantId,
        Guid subjectEmployeeId,
        Guid requestedByEmployeeId,
        Guid? feedbackProviderId,
        bool isAnonymous,
        FeedbackRequestType type,
        string? context,
        DateTimeOffset expiresOnUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (expiresOnUtc <= DateTimeOffset.UtcNow)
            throw new ArgumentException("ExpiresOnUtc must be in the future.");

        return new FeedbackRequest(Guid.NewGuid(), tenantId, subjectEmployeeId, requestedByEmployeeId,
            feedbackProviderId, isAnonymous, type, context?.Trim(), expiresOnUtc);
    }

    public void MarkSubmitted() => Transition(FeedbackRequestStatus.Submitted);
    public void Decline() => Transition(FeedbackRequestStatus.Declined, FeedbackRequestStatus.Pending);
    public void Expire() => Status = FeedbackRequestStatus.Expired;

    private void Transition(FeedbackRequestStatus to, FeedbackRequestStatus? requiredFrom = null)
    {
        if (requiredFrom.HasValue && Status != requiredFrom.Value)
            throw new InvalidOperationException($"Cannot transition to {to} from {Status}.");
        Status = to;
    }
}
