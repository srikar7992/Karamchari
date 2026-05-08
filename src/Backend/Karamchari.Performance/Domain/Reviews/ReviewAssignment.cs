using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Reviews;

/// <summary>
/// Represents one reviewee's complete set of reviewers in a cycle.
/// Each ReviewerSlot corresponds to one ReviewSubmission.
/// </summary>
public sealed class ReviewAssignment : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ReviewerSlot> _reviewerSlots = [];

    private ReviewAssignment() { /* EF materialization */ }

    private ReviewAssignment(
        Guid id,
        string tenantId,
        Guid cycleId,
        Guid revieweeId) : base(id)
    {
        TenantId = tenantId;
        CycleId = cycleId;
        RevieweeId = revieweeId;
        IsFinalized = false;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid CycleId { get; private set; }
    public Guid RevieweeId { get; private set; }
    public bool IsFinalized { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<ReviewerSlot> ReviewerSlots => _reviewerSlots.AsReadOnly();

    public static ReviewAssignment Create(string tenantId, Guid cycleId, Guid revieweeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return new ReviewAssignment(Guid.NewGuid(), tenantId, cycleId, revieweeId);
    }

    public void AddReviewer(Guid reviewerId, ReviewerRole role)
    {
        if (IsFinalized)
            throw new InvalidOperationException("Cannot modify a finalized assignment.");
        if (_reviewerSlots.Any(s => s.ReviewerId == reviewerId && s.Role == role))
            throw new InvalidOperationException($"Reviewer {reviewerId} already has role {role} in this assignment.");

        _reviewerSlots.Add(ReviewerSlot.Create(reviewerId, role));
    }

    public void RemoveReviewer(Guid reviewerId, ReviewerRole role)
    {
        if (IsFinalized)
            throw new InvalidOperationException("Cannot modify a finalized assignment.");
        var slot = _reviewerSlots.FirstOrDefault(s => s.ReviewerId == reviewerId && s.Role == role)
            ?? throw new InvalidOperationException($"Reviewer {reviewerId} with role {role} not found.");
        _reviewerSlots.Remove(slot);
    }

    public void MarkReviewerCompleted(Guid reviewerId, ReviewerRole role)
    {
        var idx = _reviewerSlots.FindIndex(s => s.ReviewerId == reviewerId && s.Role == role);
        if (idx < 0)
            throw new InvalidOperationException($"Reviewer {reviewerId} with role {role} not found.");
        _reviewerSlots[idx] = _reviewerSlots[idx].MarkCompleted();
    }

    public void FinalizeAssignment()
    {
        if (_reviewerSlots.Count == 0)
            throw new InvalidOperationException("Cannot finalize assignment with no reviewers.");
        IsFinalized = true;
    }

    public bool AllReviewersCompleted()
        => _reviewerSlots.Count > 0 && _reviewerSlots.All(s => s.IsCompleted);
}
