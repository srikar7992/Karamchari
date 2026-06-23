// -----------------------------------------------------------------------
// <copyright file="GetOfferDetailQuery.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Recruitment.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Queries;

/// <summary>
/// Loads a single offer by identifier.
/// </summary>
public record GetOfferDetailQuery(Guid Id) : IRequest<OfferDto?>;

public sealed class GetOfferDetailHandler(IRecruitmentDbContext dbContext)
    : IRequestHandler<GetOfferDetailQuery, OfferDto?>
{
    public async Task<OfferDto?> Handle(
        GetOfferDetailQuery request,
        CancellationToken cancellationToken)
    {
        var offer = await dbContext.Offers
            .AsNoTracking()
            .Where(o => o.Id == new Domain.OfferId(request.Id))
            .Select(o => new OfferDto(
                o.Id.Value,
                o.ApplicationId.Value,
                o.BaseSalary,
                o.Currency,
                o.Status.ToString(),
                o.IssuedAt,
                o.ExpiresAt))
            .FirstOrDefaultAsync(cancellationToken);

        return offer;
    }
}
