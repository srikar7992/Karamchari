using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Reviews.Events;
using Karamchari.Performance.Domain.Scoring;

namespace Karamchari.Performance.Domain.Reviews;

/// <summary>
/// One reviewer's complete response set for one assignment in a review cycle.
/// State machine: NotStarted → InProgress → Submitted → Acknowledged → Locked
///                                         ↑             ↓
///                                         └─ Reopened ←─ (HR/admin, requires justification)
/// IdempotencyKey prevents duplicate submissions from retry storms.
/// </summary>
public sealed class ReviewSubmission : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ReviewResponse> _responses = [];
    private readonly List<ReopenRecord> _reopenHistory = [];

    private ReviewSubmission() { /* EF materialization */ }

    private ReviewSubmission(
        Guid id,
        string tenantId,
        Guid assignmentId,
        Guid revieweeId,
        Guid reviewerId,
        ReviewerRole reviewerRole,
        Guid templateId,
        Guid idempotencyKey) : base(id)
    {
        TenantId = tenantId;
        AssignmentId = assignmentId;
        RevieweeId = revieweeId;
        ReviewerId = reviewerId;
        ReviewerRole = reviewerRole;
        TemplateId = templateId;
        Status = ReviewSubmissionStatus.NotStarted;
        IdempotencyKey = idempotencyKey;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid AssignmentId { get; private set; }
    public Guid RevieweeId { get; private set; }
    public Guid ReviewerId { get; private set; }
    public ReviewerRole ReviewerRole { get; private set; }
    public Guid TemplateId { get; private set; }
    public ReviewSubmissionStatus Status { get; private set; }
    public decimal? ComputedScore { get; private set; }
    public DateTimeOffset? SubmittedOnUtc { get; private set; }
    public DateTimeOffset? AcknowledgedOnUtc { get; private set; }

    /// <summary>Unique constraint on (TenantId, IdempotencyKey) prevents duplicate submissions.</summary>
    public Guid IdempotencyKey { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyList<ReviewResponse> Responses => _responses.AsReadOnly();
    public IReadOnlyList<ReopenRecord> ReopenHistory => _reopenHistory.AsReadOnly();

    public static ReviewSubmission Create(
        string tenantId,
        Guid assignmentId,
        Guid revieweeId,
        Guid reviewerId,
        ReviewerRole reviewerRole,
        Guid templateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        var key = Guid.NewGuid();
        return new ReviewSubmission(key, tenantId, assignmentId, revieweeId,
            reviewerId, reviewerRole, templateId, key);
    }

    public void RecordRatingResponse(Guid questionId, decimal rating, QuestionType type, decimal minRating, decimal maxRating)
    {
        EnsureEditable();
        if (rating < minRating || rating > maxRating)
            throw new ArgumentOutOfRangeException(nameof(rating), $"Rating must be {minRating}–{maxRating}.");

        RemoveExistingResponse(questionId);
        _responses.Add(ReviewResponse.ForRating(Id, questionId, rating, type));
        Status = ReviewSubmissionStatus.InProgress;
    }

    public void RecordTextResponse(Guid questionId, string text)
    {
        EnsureEditable();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        RemoveExistingResponse(questionId);
        _responses.Add(ReviewResponse.ForText(Id, questionId, text));
        Status = ReviewSubmissionStatus.InProgress;
    }

    public void Submit(IReviewScoringStrategy scoringStrategy, ReviewTemplate template)
    {
        if (Status != ReviewSubmissionStatus.InProgress && Status != ReviewSubmissionStatus.Reopened)
            throw new InvalidOperationException($"Cannot submit from status {Status}.");

        ArgumentNullException.ThrowIfNull(scoringStrategy);
        ArgumentNullException.ThrowIfNull(template);

        ComputedScore = scoringStrategy.ComputeSubmissionScore(_responses, template);
        Status = ReviewSubmissionStatus.Submitted;
        SubmittedOnUtc = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new ReviewSubmissionSubmitted(
            Id, TenantId, AssignmentId, RevieweeId, ReviewerId, ReviewerRole,
            ComputedScore.Value, SubmittedOnUtc.Value));
    }

    public void Acknowledge()
    {
        if (Status != ReviewSubmissionStatus.Submitted)
            throw new InvalidOperationException($"Cannot acknowledge from status {Status}.");
        Status = ReviewSubmissionStatus.Acknowledged;
        AcknowledgedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Lock()
    {
        if (Status != ReviewSubmissionStatus.Acknowledged)
            throw new InvalidOperationException($"Cannot lock from status {Status}.");
        Status = ReviewSubmissionStatus.Locked;
    }

    public void Reopen(Guid reopenedByHR, string justification)
    {
        if (Status != ReviewSubmissionStatus.Submitted && Status != ReviewSubmissionStatus.Acknowledged)
            throw new InvalidOperationException($"Cannot reopen from status {Status}.");
        ArgumentException.ThrowIfNullOrWhiteSpace(justification);

        _reopenHistory.Add(new ReopenRecord(reopenedByHR, justification, DateTimeOffset.UtcNow));
        Status = ReviewSubmissionStatus.Reopened;
    }

    private void EnsureEditable()
    {
        if (Status == ReviewSubmissionStatus.Locked)
            throw new InvalidOperationException("Cannot modify a locked submission.");
        if (Status == ReviewSubmissionStatus.Submitted || Status == ReviewSubmissionStatus.Acknowledged)
            throw new InvalidOperationException($"Cannot modify a {Status} submission. Reopen first.");
    }

    private void RemoveExistingResponse(Guid questionId)
    {
        var existing = _responses.FirstOrDefault(r => r.QuestionId == questionId);
        if (existing != null) _responses.Remove(existing);
    }
}

/// <summary>Audit trail for HR-initiated reopens.</summary>
public sealed record ReopenRecord(
    Guid ReopenedBy,
    string Justification,
    DateTimeOffset ReopenedOnUtc);
