// -----------------------------------------------------------------------
// <copyright file="CandidateHiredHandler.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Karamchari.Recruitment.Contracts;
using Karamchari.Recruitment.Domain;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.EventHandlers;

public sealed class CandidateHiredHandler(
    IRecruitmentDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    ITenantProvider tenantProvider) : INotificationHandler<CandidateHiredDomainEvent>
{
    public async Task Handle(CandidateHiredDomainEvent notification, CancellationToken cancellationToken)
    {
        var application = await dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == notification.ApplicationId, cancellationToken);

        if (application == null) return;

        var tenantId = tenantProvider.GetCurrentTenantId();

        // The hired event carries the agreed compensation, sourced from the accepted offer
        // for this application. There may be superseded (declined/expired) offers, so take the
        // accepted one; fall back to zero only if none is found (defensive — a hire should
        // always have an accepted offer).
        var acceptedOffer = await dbContext.Offers
            .AsNoTracking()
            .Where(o => o.ApplicationId == notification.ApplicationId && o.Status == OfferStatus.Accepted)
            .OrderByDescending(o => o.IssuedAt)
            .FirstOrDefaultAsync(cancellationToken);

        await publishEndpoint.Publish(new CandidateHiredIntegrationEventV1(
            notification.CandidateId.Value,
            notification.ApplicationId.Value,
            application.RequisitionId.Value,
            tenantId,
            application.CandidateSnapshot.FirstName,
            application.CandidateSnapshot.LastName,
            application.CandidateSnapshot.Email,
            application.CandidateSnapshot.PhoneNumber,
            application.HiredAt ?? DateTimeOffset.UtcNow,
            application.HiredBy ?? "system",
            acceptedOffer?.BaseSalary ?? 0m,
            acceptedOffer?.Currency ?? "USD"
        ), cancellationToken);
    }
}
