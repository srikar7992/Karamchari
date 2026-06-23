// -----------------------------------------------------------------------
// <copyright file="GetApplicationsQuery.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Recruitment.Contracts;
using Karamchari.Recruitment.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Queries;

/// <summary>
/// Lists applications for the current tenant, optionally filtered by candidate,
/// requisition, or status.
/// </summary>
public record GetApplicationsQuery(
    Guid? CandidateId = null,
    Guid? RequisitionId = null,
    string? Status = null) : IRequest<IReadOnlyList<ApplicationSummaryDto>>;

public sealed class GetApplicationsHandler(IRecruitmentDbContext dbContext)
    : IRequestHandler<GetApplicationsQuery, IReadOnlyList<ApplicationSummaryDto>>
{
    public async Task<IReadOnlyList<ApplicationSummaryDto>> Handle(
        GetApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Applications.AsNoTracking();

        if (request.CandidateId is { } candidateId)
        {
            var cid = new CandidateId(candidateId);
            query = query.Where(a => a.CandidateId == cid);
        }

        if (request.RequisitionId is { } requisitionId)
        {
            var rid = new RequisitionId(requisitionId);
            query = query.Where(a => a.RequisitionId == rid);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<ApplicationStatus>(request.Status, true, out var status))
            {
                query = query.Where(a => a.Status == status);
            }
            else
            {
                return Array.Empty<ApplicationSummaryDto>();
            }
        }

        var applications = await query
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

        return applications;
    }
}
