// -----------------------------------------------------------------------
// <copyright file="GetApplicationDetailQuery.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Recruitment.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Queries;

/// <summary>
/// Loads an application with its embedded candidate snapshot, interviews,
/// offers, and auditable lifecycle timeline.
/// </summary>
public record GetApplicationDetailQuery(Guid Id) : IRequest<ApplicationDetailDto?>;

public sealed class GetApplicationDetailHandler(IRecruitmentDbContext dbContext)
    : IRequestHandler<GetApplicationDetailQuery, ApplicationDetailDto?>
{
    public async Task<ApplicationDetailDto?> Handle(
        GetApplicationDetailQuery request,
        CancellationToken cancellationToken)
    {
        var applicationId = new Domain.ApplicationId(request.Id);

        var application = await dbContext.Applications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (application == null)
        {
            return null;
        }

        var interviews = await dbContext.Interviews
            .AsNoTracking()
            .Where(i => i.ApplicationId == applicationId)
            .OrderByDescending(i => i.ScheduledAt)
            .Select(i => new InterviewDto(
                i.Id.Value,
                i.ApplicationId.Value,
                i.ScheduledAt,
                i.DurationMinutes,
                i.Status.ToString(),
                i.InterviewerIds,
                dbContext.InterviewFeedbacks
                    .Where(f => f.InterviewId == i.Id)
                    .OrderByDescending(f => f.SubmittedAt)
                    .Select(f => new InterviewFeedbackDto(
                        f.Id.Value,
                        f.InterviewerId,
                        f.Rating,
                        f.Comments,
                        f.SubmittedAt))
                    .ToList()))
            .ToListAsync(cancellationToken);

        var offers = await dbContext.Offers
            .AsNoTracking()
            .Where(o => o.ApplicationId == applicationId)
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

        var timeline = await dbContext.AuditStream
            .AsNoTracking()
            .Where(a => a.EntityId == application.Id.Value)
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

        CandidateSnapshotDto? snapshot = null;
        if (application.CandidateSnapshot is { } snap)
        {
            snapshot = new CandidateSnapshotDto(
                snap.FirstName,
                snap.LastName,
                snap.Email,
                snap.PhoneNumber,
                snap.Version);
        }

        return new ApplicationDetailDto(
            application.Id.Value,
            application.CandidateId.Value,
            application.RequisitionId.Value,
            application.Status.ToString(),
            application.AppliedAt,
            application.HiredAt,
            application.HiredBy,
            snapshot,
            interviews,
            offers,
            timeline);
    }
}
