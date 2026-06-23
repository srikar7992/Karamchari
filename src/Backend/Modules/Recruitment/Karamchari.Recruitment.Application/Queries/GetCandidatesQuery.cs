// -----------------------------------------------------------------------
// <copyright file="GetCandidatesQuery.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Recruitment.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Queries;

/// <summary>
/// Lists every candidate in the current tenant, ordered by creation time (newest first).
/// </summary>
public record GetCandidatesQuery() : IRequest<IReadOnlyList<CandidateSummaryDto>>;

public sealed class GetCandidatesHandler(IRecruitmentDbContext dbContext)
    : IRequestHandler<GetCandidatesQuery, IReadOnlyList<CandidateSummaryDto>>
{
    public async Task<IReadOnlyList<CandidateSummaryDto>> Handle(
        GetCandidatesQuery request,
        CancellationToken cancellationToken)
    {
        var candidates = await dbContext.Candidates
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedOnUtc)
            .Select(c => new CandidateSummaryDto(
                c.Id.Value,
                c.FirstName,
                c.LastName,
                c.Email,
                c.PhoneNumber,
                c.ProfileVersion,
                c.CreatedOnUtc))
            .ToListAsync(cancellationToken);

        return candidates;
    }
}
