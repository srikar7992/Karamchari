using Karamchari.Core.Messaging;
using Karamchari.Core.Multitenancy;
using Karamchari.Recruitment.Contracts;
using Karamchari.Recruitment.Domain;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.EventHandlers;

public sealed class RecruitmentAnalyticsHandler(
    IRecruitmentDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    ITenantProvider tenantProvider) :
        INotificationHandler<RequisitionPublishedDomainEvent>,
        INotificationHandler<CandidateAppliedDomainEvent>,
        INotificationHandler<InterviewCompletedDomainEvent>,
        INotificationHandler<OfferAcceptedDomainEvent>,
        INotificationHandler<CandidateHiredDomainEvent>
{
    private const string Producer = "karamchari.recruitment";

    public async Task Handle(RequisitionPublishedDomainEvent notification, CancellationToken cancellationToken)
    {
        var req = await dbContext.Requisitions.FirstOrDefaultAsync(r => r.Id == notification.RequisitionId, cancellationToken);
        if (req == null) return;

        var integrationEvent = new RequisitionPublishedIntegrationEvent(
            req.Id.Value,
            tenantProvider.GetCurrentTenantId(),
            req.Title,
            req.DepartmentId.ToString(),
            DateTimeOffset.UtcNow);

        await PublishWrapped(integrationEvent, "RequisitionPublished", cancellationToken);
    }

    public async Task Handle(CandidateAppliedDomainEvent notification, CancellationToken cancellationToken)
    {
        var app = await dbContext.Applications.FirstOrDefaultAsync(a => a.Id == notification.ApplicationId, cancellationToken);
        if (app == null) return;

        var integrationEvent = new CandidateAppliedIntegrationEvent(
            app.Id.Value,
            app.CandidateId.Value,
            app.RequisitionId.Value,
            tenantProvider.GetCurrentTenantId(),
            $"{app.CandidateSnapshot.FirstName} {app.CandidateSnapshot.LastName}",
            "Position", // TODO: Pull Req Title
            app.AppliedAt);

        await PublishWrapped(integrationEvent, "CandidateApplied", cancellationToken);
    }

    public async Task Handle(InterviewCompletedDomainEvent notification, CancellationToken cancellationToken)
    {
        var interview = await dbContext.Interviews.FirstOrDefaultAsync(i => i.Id == notification.InterviewId, cancellationToken);
        if (interview == null) return;

        var integrationEvent = new InterviewCompletedIntegrationEvent(
            interview.Id.Value,
            interview.ApplicationId.Value,
            tenantProvider.GetCurrentTenantId(),
            DateTimeOffset.UtcNow);

        await PublishWrapped(integrationEvent, "InterviewCompleted", cancellationToken);
    }

    public async Task Handle(OfferAcceptedDomainEvent notification, CancellationToken cancellationToken)
    {
        var offer = await dbContext.Offers.FirstOrDefaultAsync(o => o.Id == notification.OfferId, cancellationToken);
        if (offer == null) return;

        var integrationEvent = new OfferAcceptedIntegrationEvent(
            offer.Id.Value,
            offer.ApplicationId.Value,
            tenantProvider.GetCurrentTenantId(),
            DateTimeOffset.UtcNow);

        await PublishWrapped(integrationEvent, "OfferAccepted", cancellationToken);
    }

    public async Task Handle(CandidateHiredDomainEvent notification, CancellationToken cancellationToken)
    {
        // Handled in specific CandidateHiredHandler for HR integration,
        // but can also emit to Analytics stream here.
    }

    private async Task PublishWrapped<T>(T payload, string eventType, CancellationToken ct) where T : class
    {
        var envelope = EnterpriseEventEnvelope<T>.Wrap(
            payload,
            tenantProvider.GetCurrentTenantId(),
            eventType,
            producer: Producer);

        await publishEndpoint.Publish(envelope, ct);
    }
}
