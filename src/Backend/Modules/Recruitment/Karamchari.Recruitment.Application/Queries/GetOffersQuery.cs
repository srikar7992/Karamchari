// -----------------------------------------------------------------------
// <copyright file="GetOffersQuery.cs" company="Karamchari">
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
/// Lists offers for the current tenant, optionally filtered by application or status.
/// </summary>
public record GetOffersQuery(
    Guid? ApplicationId = null,
    string? Status = null) : IRequest<IReadOnlyList<OfferDto>>;

public sealed class GetOffersHandler(IRecruitmentDbContext dbContext)
    : IRequestHandler<GetOffersQuery, IReadOnlyList<OfferDto>>
{
    public async Task<IReadOnlyList<OfferDto>> Handle(
        GetOffersQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Offers.AsNoTracking();

        if (request.ApplicationId is { } applicationId)
        {
            var aid = new Karamchari.Recruitment.Domain.ApplicationId(applicationId);
            query = query.Where(o => o.ApplicationId == aid);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<OfferStatus>(request.Status, true, out var status))
            {
                query = query.Where(o => o.Status == status);
            }
            else
            {
                return Array.Empty<OfferDto>();
            }
        }

        var offers = await query
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

        return offers;
    }
}
