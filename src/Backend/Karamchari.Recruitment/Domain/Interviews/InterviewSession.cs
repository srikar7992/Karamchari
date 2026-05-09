using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Recruitment.Domain.Interviews;

public enum InterviewStatus
{
    Scheduled,
    Completed,
    Cancelled,
    NoShow
}

/// <summary>
/// Aggregate root representing an interview session.
/// Enforces timezone-aware scheduling and feedback locking.
/// </summary>
public sealed class InterviewSession : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<InterviewFeedback> _feedback = [];

    public string TenantId { get; private set; } = string.Empty;
    public Guid ApplicationId { get; private set; }
    public Guid CandidateId { get; private set; }
    
    // Explicit UTC storage to prevent cross-region timezone overlap errors
    public DateTimeOffset StartTimeUtc { get; private set; }
    public DateTimeOffset EndTimeUtc { get; private set; }
    
    public InterviewStatus Status { get; private set; }
    public string MeetingLink { get; private set; } = string.Empty;
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<InterviewFeedback> Feedback => _feedback.AsReadOnly();

    private InterviewSession() { }

    public static InterviewSession Schedule(
        string tenantId,
        Guid applicationId,
        Guid candidateId,
        DateTimeOffset startTimeUtc,
        DateTimeOffset endTimeUtc,
        string meetingLink)
    {
        if (endTimeUtc <= startTimeUtc)
            throw new ArgumentException("End time must be after start time.");

        return new InterviewSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ApplicationId = applicationId,
            CandidateId = candidateId,
            StartTimeUtc = startTimeUtc,
            EndTimeUtc = endTimeUtc,
            MeetingLink = meetingLink,
            Status = InterviewStatus.Scheduled
        };
    }

    public void AssignInterviewer(Guid interviewerId)
    {
        if (Status != InterviewStatus.Scheduled)
            throw new InvalidOperationException($"Cannot assign interviewer to a {Status} interview.");

        if (_feedback.Any(f => f.InterviewerId == interviewerId))
            throw new InvalidOperationException("Interviewer is already assigned.");

        _feedback.Add(InterviewFeedback.Create(Id, interviewerId));
    }

    public void SubmitFeedback(Guid interviewerId, string scorecardJson, int overallScore)
    {
        if (Status != InterviewStatus.Scheduled && Status != InterviewStatus.Completed)
            throw new InvalidOperationException($"Cannot submit feedback for a {Status} interview.");

        var feedback = _feedback.FirstOrDefault(f => f.InterviewerId == interviewerId)
            ?? throw new InvalidOperationException("Interviewer is not assigned to this session.");

        feedback.Submit(scorecardJson, overallScore);
        
        // Auto-mark as completed if this is the first feedback and it's past start time
        if (Status == InterviewStatus.Scheduled && DateTimeOffset.UtcNow > StartTimeUtc)
        {
            Status = InterviewStatus.Completed;
        }
    }

    public void Cancel(string reason)
    {
        if (Status != InterviewStatus.Scheduled)
            throw new InvalidOperationException($"Cannot cancel a {Status} interview.");

        Status = InterviewStatus.Cancelled;
        // Logic for raising cancellation event to notify candidate and interviewers
    }
}

public sealed class InterviewFeedback : Entity<Guid>
{
    public Guid SessionId { get; private set; }
    public Guid InterviewerId { get; private set; }
    public string? ScorecardJson { get; private set; }
    public int? OverallScore { get; private set; }
    public bool IsSubmitted { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }

    private InterviewFeedback() { }

    internal static InterviewFeedback Create(Guid sessionId, Guid interviewerId)
    {
        return new InterviewFeedback
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            InterviewerId = interviewerId,
            IsSubmitted = false
        };
    }

    internal void Submit(string scorecardJson, int score)
    {
        if (IsSubmitted)
            throw new InvalidOperationException("Feedback is already submitted and locked.");

        ScorecardJson = scorecardJson;
        OverallScore = score;
        IsSubmitted = true;
        SubmittedAtUtc = DateTimeOffset.UtcNow;
    }
}
