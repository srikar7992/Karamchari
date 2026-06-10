// -----------------------------------------------------------------------
// <copyright file="HiringFunnelConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Karamchari.Core.Messaging;
using Karamchari.Recruitment.Application.Analytics;
using Karamchari.Recruitment.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Recruitment.Application.EventHandlers.Analytics;

public sealed class HiringFunnelConsumer(
    IRecruitmentDbContext dbContext,
    ILogger<HiringFunnelConsumer> logger)
    : IConsumer<EnterpriseEventEnvelope<CandidateAppliedIntegrationEvent>>,
      IConsumer<EnterpriseEventEnvelope<InterviewCompletedIntegrationEvent>>,
      IConsumer<EnterpriseEventEnvelope<OfferAcceptedIntegrationEvent>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<CandidateAppliedIntegrationEvent>> context)
    {
        var envelope = context.Message;
        var payload = envelope.Payload;
        const string eventType = "ApplicationSubmitted";

        logger.LogInformation(
            "HiringFunnelConsumer: processing {EventType} for application {ApplicationId} candidate {CandidateId} tenant {TenantId}",
            eventType, payload.ApplicationId, payload.CandidateId, envelope.TenantId);

        var alreadyProcessed = await dbContext.AnalyticsReadModels.AnyAsync(
            a => a.EventType == eventType
              && a.ApplicationId == payload.ApplicationId
              && a.CandidateId == payload.CandidateId
              && a.TenantId == envelope.TenantId,
            context.CancellationToken);

        if (alreadyProcessed)
        {
            logger.LogDebug(
                "HiringFunnelConsumer: duplicate ApplicationSubmitted event skipped for application {ApplicationId}",
                payload.ApplicationId);
            return;
        }

        var model = new AnalyticsReadModel
        {
            Id = Guid.NewGuid(),
            TenantId = envelope.TenantId,
            EventType = eventType,
            RequisitionId = payload.RequisitionId,
            CandidateId = payload.CandidateId,
            ApplicationId = payload.ApplicationId,
            EventTimestamp = payload.AppliedAt,
            AdditionalData = JsonSerializer.Serialize(new
            {
                payload.CandidateName,
                payload.RequisitionTitle,
                EventId = envelope.EventId,
                CorrelationId = envelope.CorrelationId
            }),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.AnalyticsReadModels.Add(model);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "HiringFunnelConsumer: persisted analytics row {Id} for application {ApplicationId}",
            model.Id, payload.ApplicationId);
    }

    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<InterviewCompletedIntegrationEvent>> context)
    {
        var envelope = context.Message;
        var payload = envelope.Payload;
        const string eventType = "InterviewCompleted";

        logger.LogInformation(
            "HiringFunnelConsumer: processing {EventType} for interview {InterviewId} application {ApplicationId} tenant {TenantId}",
            eventType, payload.InterviewId, payload.ApplicationId, envelope.TenantId);

        var alreadyProcessed = await dbContext.AnalyticsReadModels.AnyAsync(
            a => a.EventType == eventType
              && a.ApplicationId == payload.ApplicationId
              && a.TenantId == envelope.TenantId,
            context.CancellationToken);

        if (alreadyProcessed)
        {
            logger.LogDebug(
                "HiringFunnelConsumer: duplicate InterviewCompleted event skipped for application {ApplicationId}",
                payload.ApplicationId);
            return;
        }

        var model = new AnalyticsReadModel
        {
            Id = Guid.NewGuid(),
            TenantId = envelope.TenantId,
            EventType = eventType,
            RequisitionId = null,
            CandidateId = null,
            ApplicationId = payload.ApplicationId,
            EventTimestamp = payload.CompletedAt,
            AdditionalData = JsonSerializer.Serialize(new
            {
                InterviewId = payload.InterviewId,
                CompletedAt = payload.CompletedAt,
                EventId = envelope.EventId,
                CorrelationId = envelope.CorrelationId
            }),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.AnalyticsReadModels.Add(model);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "HiringFunnelConsumer: persisted analytics row {Id} for application {ApplicationId}",
            model.Id, payload.ApplicationId);
    }

    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<OfferAcceptedIntegrationEvent>> context)
    {
        var envelope = context.Message;
        var payload = envelope.Payload;
        const string eventType = "OfferAccepted";

        logger.LogInformation(
            "HiringFunnelConsumer: processing {EventType} for application {ApplicationId} tenant {TenantId}",
            eventType, payload.ApplicationId, envelope.TenantId);

        var alreadyProcessed = await dbContext.AnalyticsReadModels.AnyAsync(
            a => a.EventType == eventType
              && a.ApplicationId == payload.ApplicationId
              && a.TenantId == envelope.TenantId,
            context.CancellationToken);

        if (alreadyProcessed)
        {
            logger.LogDebug(
                "HiringFunnelConsumer: duplicate OfferAccepted event skipped for application {ApplicationId}",
                payload.ApplicationId);
            return;
        }

        var model = new AnalyticsReadModel
        {
            Id = Guid.NewGuid(),
            TenantId = envelope.TenantId,
            EventType = eventType,
            RequisitionId = null,
            CandidateId = null,
            ApplicationId = payload.ApplicationId,
            EventTimestamp = payload.AcceptedAt,
            AdditionalData = JsonSerializer.Serialize(new
            {
                OfferId = payload.OfferId,
                AcceptedAt = payload.AcceptedAt,
                EventId = envelope.EventId,
                CorrelationId = envelope.CorrelationId
            }),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.AnalyticsReadModels.Add(model);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "HiringFunnelConsumer: persisted analytics row {Id} for application {ApplicationId}",
            model.Id, payload.ApplicationId);
    }
}
