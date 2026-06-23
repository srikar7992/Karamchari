// -----------------------------------------------------------------------
// <copyright file="GetCandidateDetailQuery.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Recruitment.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Queries;

/// <summary>
/// Loads the full candidate record: profile, related applications, their interviews,
/// candidate offers, and the auditable lifecycle timeline.
/// </summary>
public record GetCandidateDetailQuery(Guid Id) : IRequest<CandidateDetailDto?>;

public sealed class GetCandidateDetailHandler(IRecruitmentDbContext dbContext)
    : IRequestHandler<GetCandidateDetailQuery, CandidateDetailDto?>
{
    public async Task<CandidateDetailDto?> Handle(
        GetCandidateDetailQuery request,
        CancellationToken cancellationToken)
    {
        var candidateId = new Domain.CandidateId(request.Id);

        var candidate = await dbContext.Candidates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == candidateId, cancellationToken);

        if (candidate == null)
        {
            return null;
        }

        var applications = await dbContext.Applications
            .AsNoTracking()
            .Where(a => a.CandidateId == candidateId)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new ApplicationSummaryDto(
                a.Id.Value,
                a.CandidateId.Value,
                a.RequisitionId.Value,
                a.Status.ToString(),
                a.AppliedAt,
                a.HiredAt,
                a.HiredBy))
            .ToListAsync(cancellationToken);

        var applicationIdValues = applications.Select(a => a.Id).ToList();

        // Filter interviews/offers via correlated subqueries against Applications scoped
        // to this candidate. EF translates `==` over strongly-typed ID wrappers (via
        // HasConversion) but cannot translate `.Value` member access inside Contains()
        // over a client list, so the subquery form is mandatory here.
        var interviews = await dbContext.Interviews
            .AsNoTracking()
            .Where(i => dbContext.Applications.Any(a => a.CandidateId == candidateId && a.Id == i.ApplicationId))
            .OrderByDescending(i => i.ScheduledAt)
            .Select(i => new
            {
                Id = i.Id.Value,
                ApplicationId = i.ApplicationId.Value,
                i.ScheduledAt,
                i.DurationMinutes,
                Status = i.Status.ToString(),
                i.InterviewerIds,
            })
            .ToListAsync(cancellationToken);

        var interviewIds = interviews.Select(i => new Domain.InterviewId(i.Id)).ToList();

        var feedbacks = await dbContext.InterviewFeedbacks
            .AsNoTracking()
            .Where(f => interviewIds.Contains(f.InterviewId))
            .OrderByDescending(f => f.SubmittedAt)
            .Select(f => new
            {
                InterviewId = f.InterviewId.Value,
                Feedback = new InterviewFeedbackDto(
                    f.Id.Value,
                    f.InterviewerId,
                    f.Rating,
                    f.Comments,
                    f.SubmittedAt),
            })
            .ToListAsync(cancellationToken);

        var feedbackByInterview = feedbacks
            .GroupBy(f => f.InterviewId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Feedback).ToList());

        var interviewDtos = interviews
            .Select(i => new InterviewDto(
                i.Id,
                i.ApplicationId,
                i.ScheduledAt,
                i.DurationMinutes,
                i.Status,
                i.InterviewerIds,
                feedbackByInterview.TryGetValue(i.Id, out var fb) ? fb : new List<InterviewFeedbackDto>()))
            .ToList();

        var offers = await dbContext.Offers
            .AsNoTracking()
            .Where(o => dbContext.Applications.Any(a => a.CandidateId == candidateId && a.Id == o.ApplicationId))
            .OrderByDescending(o => o.CreatedOnUtc)
            .Select(o => new OfferDto(
                o.Id.Value,
                o.ApplicationId.Value,
                o.BaseSalary,
                o.Currency,
                o.Status.ToString(),
                o.IssuedAt,
                o.ExpiresAt))
            .ToListAsync(cancellationToken);

        var timeline = await LoadTimelineAsync(candidate.Id.Value, applicationIdValues, cancellationToken);

        return new CandidateDetailDto(
            candidate.Id.Value,
            candidate.FirstName,
            candidate.LastName,
            candidate.Email,
            candidate.PhoneNumber,
            candidate.ProfileVersion,
            candidate.CreatedOnUtc,
            candidate.UpdatedOnUtc,
            applications,
            interviewDtos,
            offers,
            timeline);
    }

    private async Task<IReadOnlyList<TimelineEntryDto>> LoadTimelineAsync(
        Guid candidateIdValue,
        IReadOnlyList<Guid> applicationIds,
        CancellationToken cancellationToken)
    {
        var entityIds = new List<Guid> { candidateIdValue };
        entityIds.AddRange(applicationIds);

        var entries = await dbContext.AuditStream
            .AsNoTracking()
            .Where(a => entityIds.Contains(a.EntityId))
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new TimelineEntryDto(
                a.Id,
                a.EntityType,
                a.EntityId,
                a.Action,
                a.OldState,
                a.NewState,
                a.Timestamp,
                a.UserId))
            .ToListAsync(cancellationToken);

        return entries;
    }
}
