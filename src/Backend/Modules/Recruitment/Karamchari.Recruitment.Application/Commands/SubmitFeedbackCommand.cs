using System.Text.Json;
using Karamchari.Recruitment.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Commands;

public record SubmitFeedbackCommand(
    InterviewId InterviewId,
    Guid InterviewerId,
    int Rating,
    string Comments) : IRequest<InterviewFeedbackId>;

public sealed class SubmitFeedbackHandler(IRecruitmentDbContext dbContext) 
    : IRequestHandler<SubmitFeedbackCommand, InterviewFeedbackId>
{
    public async Task<InterviewFeedbackId> Handle(SubmitFeedbackCommand request, CancellationToken cancellationToken)
    {
        var interview = await dbContext.Interviews
            .FirstOrDefaultAsync(i => i.Id == request.InterviewId, cancellationToken)
            ?? throw new InvalidOperationException($"Interview {request.InterviewId.Value} not found.");
        
        if (interview.Status != InterviewStatus.Scheduled)
            throw new InvalidOperationException("Feedback can only be submitted for scheduled interviews.");

        if (!interview.InterviewerIds.Contains(request.InterviewerId))
            throw new InvalidOperationException("Interviewer is not assigned to this interview.");

        var feedback = InterviewFeedback.Create(request.InterviewId, request.InterviewerId, request.Rating, request.Comments);
        
        // Auto-complete interview on first feedback for simplicity in Sprint 2
        interview.Complete();

        dbContext.InterviewFeedbacks.Add(feedback);

        var auditEntry = new RecruitmentAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityId = interview.Id.Value,
            EntityType = "Interview",
            Action = "FeedbackSubmitted",
            NewState = JsonSerializer.Serialize(interview),
            Timestamp = DateTimeOffset.UtcNow,
            UserId = "system"
        };
        dbContext.AuditStream.Add(auditEntry);

        await dbContext.SaveChangesAsync(cancellationToken);

        return feedback.Id;
    }
}
