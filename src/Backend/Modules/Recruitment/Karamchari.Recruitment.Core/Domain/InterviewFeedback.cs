using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Recruitment.Domain;

public sealed class InterviewFeedback : AggregateRoot<InterviewFeedbackId>, ITenantOwned, IAuditable
{
    private InterviewFeedback() : base(default) { Comments = null!; }

    private InterviewFeedback(InterviewFeedbackId id, InterviewId interviewId, Guid interviewerId, int rating, string comments)
        : base(id)
    {
        InterviewId = interviewId;
        InterviewerId = interviewerId;
        Rating = rating;
        Comments = comments;
        SubmittedAt = DateTimeOffset.UtcNow;
    }

    public InterviewId InterviewId { get; private set; }
    public Guid InterviewerId { get; private set; }
    public int Rating { get; private set; }
    public string Comments { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }

    // ITenantOwned
    public string TenantId { get; private set; } = null!;

    // IAuditable
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset? UpdatedOnUtc { get; private set; }
    public string? UpdatedBy { get; private set; }

    public static InterviewFeedback Create(InterviewId interviewId, Guid interviewerId, int rating, string comments)
    {
        var feedback = new InterviewFeedback(InterviewFeedbackId.New(), interviewId, interviewerId, rating, comments);
        feedback.RaiseDomainEvent(new InterviewFeedbackSubmittedDomainEvent(feedback.Id, interviewId));
        return feedback;
    }
}
