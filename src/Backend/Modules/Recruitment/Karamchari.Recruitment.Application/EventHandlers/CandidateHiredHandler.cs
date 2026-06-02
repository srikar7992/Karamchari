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

        await publishEndpoint.Publish(new CandidateHiredIntegrationEvent(
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
            0, // TODO: Pull from accepted Offer
            "USD"
        ), cancellationToken);
    }
}
