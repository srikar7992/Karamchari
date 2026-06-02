using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Recruitment.Domain;

public sealed class Interview : AggregateRoot<InterviewId>, ITenantOwned, IAuditable
{
    private Interview() : base(default) { }

    private Interview(InterviewId id, ApplicationId applicationId, DateTimeOffset scheduledAt, int durationMinutes)
        : base(id)
    {
        ApplicationId = applicationId;
        ScheduledAt = scheduledAt;
        DurationMinutes = durationMinutes;
        Status = InterviewStatus.Scheduled;
    }

    public ApplicationId ApplicationId { get; private set; }
    public DateTimeOffset ScheduledAt { get; private set; }
    public int DurationMinutes { get; private set; }
    public InterviewStatus Status { get; private set; }
    public List<Guid> InterviewerIds { get; private set; } = [];

    // ITenantOwned
    public string TenantId { get; private set; } = null!;

    // IAuditable
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset? UpdatedOnUtc { get; private set; }
    public string? UpdatedBy { get; private set; }

    public static Interview Create(ApplicationId applicationId, DateTimeOffset scheduledAt, int durationMinutes)
    {
        var interview = new Interview(InterviewId.New(), applicationId, scheduledAt, durationMinutes);
        interview.RaiseDomainEvent(new InterviewScheduledDomainEvent(interview.Id, applicationId));
        return interview;
    }

    public void AddInterviewer(Guid interviewerId)
    {
        if (!InterviewerIds.Contains(interviewerId))
            InterviewerIds.Add(interviewerId);
    }

    public void Complete()
    {
        if (Status != InterviewStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled interviews can be completed.");

        Status = InterviewStatus.Completed;
        RaiseDomainEvent(new InterviewCompletedDomainEvent(Id));
    }

    public void Cancel()
    {
        Status = InterviewStatus.Cancelled;
    }
}
