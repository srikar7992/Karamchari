// -----------------------------------------------------------------------
// <copyright file="RecruitmentVelocityConsumer.cs" company="Karamchari">
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

public sealed class RecruitmentVelocityConsumer(
    IRecruitmentDbContext dbContext,
    ILogger<RecruitmentVelocityConsumer> logger)
    : IConsumer<EnterpriseEventEnvelope<RequisitionPublishedIntegrationEvent>>
{
    private const string EventType = "RequisitionCreated";

    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<RequisitionPublishedIntegrationEvent>> context)
    {
        var envelope = context.Message;
        var payload = envelope.Payload;

        logger.LogInformation(
            "RecruitmentVelocityConsumer: processing {EventType} for requisition {RequisitionId} tenant {TenantId}",
            EventType, payload.RequisitionId, envelope.TenantId);

        var alreadyProcessed = await dbContext.AnalyticsReadModels.AnyAsync(
            a => a.EventType == EventType
              && a.RequisitionId == payload.RequisitionId
              && a.TenantId == envelope.TenantId,
            context.CancellationToken);

        if (alreadyProcessed)
        {
            logger.LogDebug(
                "RecruitmentVelocityConsumer: duplicate event skipped for requisition {RequisitionId}",
                payload.RequisitionId);
            return;
        }

        var model = new AnalyticsReadModel
        {
            Id = Guid.NewGuid(),
            TenantId = envelope.TenantId,
            EventType = EventType,
            RequisitionId = payload.RequisitionId,
            CandidateId = null,
            ApplicationId = null,
            EventTimestamp = payload.PublishedAt,
            AdditionalData = JsonSerializer.Serialize(new
            {
                payload.Title,
                payload.DepartmentId,
                EventId = envelope.EventId,
                CorrelationId = envelope.CorrelationId
            }),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.AnalyticsReadModels.Add(model);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "RecruitmentVelocityConsumer: persisted analytics row {Id} for requisition {RequisitionId}",
            model.Id, payload.RequisitionId);
    }
}
