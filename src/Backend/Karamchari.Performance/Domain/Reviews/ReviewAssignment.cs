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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid CycleId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RevieweeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsFinalized { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<ReviewerSlot> ReviewerSlots => _reviewerSlots.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static ReviewAssignment Create(string tenantId, Guid cycleId, Guid revieweeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return new ReviewAssignment(Guid.NewGuid(), tenantId, cycleId, revieweeId);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void AddReviewer(Guid reviewerId, ReviewerRole role)
    {
        if (IsFinalized)
            throw new InvalidOperationException("Cannot modify a finalized assignment.");
        if (_reviewerSlots.Any(s => s.ReviewerId == reviewerId && s.Role == role))
            throw new InvalidOperationException($"Reviewer {reviewerId} already has role {role} in this assignment.");

        _reviewerSlots.Add(ReviewerSlot.Create(reviewerId, role));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void RemoveReviewer(Guid reviewerId, ReviewerRole role)
    {
        if (IsFinalized)
            throw new InvalidOperationException("Cannot modify a finalized assignment.");
        var slot = _reviewerSlots.FirstOrDefault(s => s.ReviewerId == reviewerId && s.Role == role)
            ?? throw new InvalidOperationException($"Reviewer {reviewerId} with role {role} not found.");
        _reviewerSlots.Remove(slot);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkReviewerCompleted(Guid reviewerId, ReviewerRole role)
    {
        var idx = _reviewerSlots.FindIndex(s => s.ReviewerId == reviewerId && s.Role == role);
        if (idx < 0)
            throw new InvalidOperationException($"Reviewer {reviewerId} with role {role} not found.");
        _reviewerSlots[idx] = _reviewerSlots[idx].MarkCompleted();
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void FinalizeAssignment()
    {
        if (_reviewerSlots.Count == 0)
            throw new InvalidOperationException("Cannot finalize assignment with no reviewers.");
        IsFinalized = true;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool AllReviewersCompleted()
        => _reviewerSlots.Count > 0 && _reviewerSlots.All(s => s.IsCompleted);
}
