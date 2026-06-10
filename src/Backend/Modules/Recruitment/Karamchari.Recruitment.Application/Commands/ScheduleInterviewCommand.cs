// -----------------------------------------------------------------------
// <copyright file="ScheduleInterviewCommand.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Karamchari.Recruitment.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Commands;

public record ScheduleInterviewCommand(
    Karamchari.Recruitment.Domain.ApplicationId ApplicationId,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    List<Guid> InterviewerIds) : IRequest<InterviewId>;

public sealed class ScheduleInterviewHandler(IRecruitmentDbContext dbContext)
    : IRequestHandler<ScheduleInterviewCommand, InterviewId>
{
    public async Task<InterviewId> Handle(ScheduleInterviewCommand request, CancellationToken cancellationToken)
    {
        var application = await dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken)
            ?? throw new InvalidOperationException($"Application {request.ApplicationId.Value} not found.");

        if (application.Status != ApplicationStatus.Screening && application.Status != ApplicationStatus.Interviewing)
            throw new InvalidOperationException("Application must be in screening or interviewing stage to schedule an interview.");

        var interview = Interview.Create(request.ApplicationId, request.ScheduledAt, request.DurationMinutes);
        foreach (var interviewerId in request.InterviewerIds)
        {
            interview.AddInterviewer(interviewerId);
        }

        if (application.Status == ApplicationStatus.Screening)
        {
            application.AdvanceToInterviewing();
        }

        dbContext.Interviews.Add(interview);

        var auditEntry = new RecruitmentAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityId = interview.Id.Value,
            EntityType = "Interview",
            Action = "Scheduled",
            NewState = JsonSerializer.Serialize(interview),
            Timestamp = DateTimeOffset.UtcNow,
            UserId = "system"
        };
        dbContext.AuditStream.Add(auditEntry);

        await dbContext.SaveChangesAsync(cancellationToken);

        return interview.Id;
    }
}
