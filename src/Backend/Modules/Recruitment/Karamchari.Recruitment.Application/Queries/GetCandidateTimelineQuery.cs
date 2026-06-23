// -----------------------------------------------------------------------
// <copyright file="GetCandidateTimelineQuery.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Recruitment.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Queries;

/// <summary>
/// Returns the auditable lifecycle timeline for a candidate, aggregating audit entries
/// against the candidate itself plus any of their applications.
/// </summary>
public record GetCandidateTimelineQuery(Guid Id) : IRequest<IReadOnlyList<TimelineEntryDto>>;

public sealed class GetCandidateTimelineHandler(IRecruitmentDbContext dbContext)
    : IRequestHandler<GetCandidateTimelineQuery, IReadOnlyList<TimelineEntryDto>>
{
    public async Task<IReadOnlyList<TimelineEntryDto>> Handle(
        GetCandidateTimelineQuery request,
        CancellationToken cancellationToken)
    {
        var candidateId = new Domain.CandidateId(request.Id);

        var candidateExists = await dbContext.Candidates
            .AsNoTracking()
            .AnyAsync(c => c.Id == candidateId, cancellationToken);

        if (!candidateExists)
        {
            return Array.Empty<TimelineEntryDto>();
        }

        var applicationIds = await dbContext.Applications
            .AsNoTracking()
            .Where(a => a.CandidateId == candidateId)
            .Select(a => a.Id.Value)
            .ToListAsync(cancellationToken);

        var entityIds = new List<Guid> { request.Id };
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
