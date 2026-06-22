// -----------------------------------------------------------------------
// <copyright file="TimeToHireConsumer.cs" company="Karamchari">
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

public sealed class TimeToHireConsumer(
    IRecruitmentDbContext dbContext,
    ILogger<TimeToHireConsumer> logger)
    : IConsumer<EnterpriseEventEnvelope<CandidateHiredIntegrationEventV1>>,
      IConsumer<EnterpriseEventEnvelope<OfferAcceptedIntegrationEventV1>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<CandidateHiredIntegrationEventV1>> context)
    {
        var envelope = context.Message;
        var payload = envelope.Payload;
        const string eventType = "CandidateHired";

        logger.LogInformation(
            "TimeToHireConsumer: processing {EventType} for candidate {CandidateId} requisition {RequisitionId} tenant {TenantId}",
            eventType, payload.CandidateId, payload.RequisitionId, envelope.TenantId);

        var alreadyProcessed = await dbContext.AnalyticsReadModels.AnyAsync(
            a => a.EventType == eventType
              && a.CandidateId == payload.CandidateId
              && a.RequisitionId == payload.RequisitionId
              && a.TenantId == envelope.TenantId,
            context.CancellationToken);

        if (alreadyProcessed)
        {
            logger.LogDebug(
                "TimeToHireConsumer: duplicate CandidateHired event skipped for candidate {CandidateId}",
                payload.CandidateId);
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
            EventTimestamp = payload.HiredAt,
            AdditionalData = JsonSerializer.Serialize(new
            {
                payload.HiredBy,
                payload.BaseSalary,
                payload.Currency,
                HiredAt = payload.HiredAt,
                EventId = envelope.EventId,
                CorrelationId = envelope.CorrelationId
            }),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.AnalyticsReadModels.Add(model);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "TimeToHireConsumer: persisted analytics row {Id} for candidate {CandidateId}",
            model.Id, payload.CandidateId);
    }

    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<OfferAcceptedIntegrationEventV1>> context)
    {
        var envelope = context.Message;
        var payload = envelope.Payload;
        const string eventType = "OfferAccepted";

        logger.LogInformation(
            "TimeToHireConsumer: processing {EventType} for application {ApplicationId} tenant {TenantId}",
            eventType, payload.ApplicationId, envelope.TenantId);

        var alreadyProcessed = await dbContext.AnalyticsReadModels.AnyAsync(
            a => a.EventType == eventType
              && a.ApplicationId == payload.ApplicationId
              && a.TenantId == envelope.TenantId,
            context.CancellationToken);

        if (alreadyProcessed)
        {
            logger.LogDebug(
                "TimeToHireConsumer: duplicate OfferAccepted event skipped for application {ApplicationId}",
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
            "TimeToHireConsumer: persisted analytics row {Id} for application {ApplicationId}",
            model.Id, payload.ApplicationId);
    }
}
