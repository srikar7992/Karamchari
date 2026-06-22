using Karamchari.Core.Messaging;
using Karamchari.Intelligence.Domain.Analytics;
using Karamchari.Intelligence.Persistence;
using Karamchari.Recruitment.Contracts;
using MassTransit;

namespace Karamchari.Intelligence.Consumers;

/// <summary>
/// Materializes a velocity measurement row for every recruitment pipeline stage event.
/// Each event is recorded as Value = 1.0 with the stage name stamped so downstream
/// queries can derive inter-stage elapsed times by comparing OccurredAt across stages.
/// </summary>
public sealed class RecruitmentVelocityConsumer(IntelligenceDbContext dbContext) :
    IConsumer<EnterpriseEventEnvelope<RequisitionPublishedIntegrationEventV1>>,
    IConsumer<EnterpriseEventEnvelope<CandidateAppliedIntegrationEventV1>>,
    IConsumer<EnterpriseEventEnvelope<InterviewCompletedIntegrationEventV1>>,
    IConsumer<EnterpriseEventEnvelope<OfferAcceptedIntegrationEventV1>>,
    IConsumer<EnterpriseEventEnvelope<CandidateHiredIntegrationEventV1>>
{
    private const string MetricType = "RecruitmentVelocity";

    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<RequisitionPublishedIntegrationEventV1>> context)
    {
        var envelope = context.Message;
        dbContext.AnalyticsReadModels.Add(new AnalyticsReadModel
        {
            Id = Guid.NewGuid(),
            TenantId = envelope.TenantId,
            MetricType = MetricType,
            EntityId = envelope.Payload.RequisitionId,
            Stage = "Published",
            Value = 1m,
            OccurredAt = envelope.Payload.PublishedAt,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<CandidateAppliedIntegrationEventV1>> context)
    {
        var envelope = context.Message;
        dbContext.AnalyticsReadModels.Add(new AnalyticsReadModel
        {
            Id = Guid.NewGuid(),
            TenantId = envelope.TenantId,
            MetricType = MetricType,
            EntityId = envelope.Payload.RequisitionId,
            Stage = "Applied",
            Value = 1m,
            OccurredAt = envelope.Payload.AppliedAt,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<InterviewCompletedIntegrationEventV1>> context)
    {
        var envelope = context.Message;
        dbContext.AnalyticsReadModels.Add(new AnalyticsReadModel
        {
            Id = Guid.NewGuid(),
            TenantId = envelope.TenantId,
            MetricType = MetricType,
            EntityId = envelope.Payload.ApplicationId,
            Stage = "InterviewCompleted",
            Value = 1m,
            OccurredAt = envelope.Payload.CompletedAt,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<OfferAcceptedIntegrationEventV1>> context)
    {
        var envelope = context.Message;
        dbContext.AnalyticsReadModels.Add(new AnalyticsReadModel
        {
            Id = Guid.NewGuid(),
            TenantId = envelope.TenantId,
            MetricType = MetricType,
            EntityId = envelope.Payload.ApplicationId,
            Stage = "OfferAccepted",
            Value = 1m,
            OccurredAt = envelope.Payload.AcceptedAt,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<CandidateHiredIntegrationEventV1>> context)
    {
        var envelope = context.Message;
        dbContext.AnalyticsReadModels.Add(new AnalyticsReadModel
        {
            Id = Guid.NewGuid(),
            TenantId = envelope.TenantId,
            MetricType = MetricType,
            EntityId = envelope.Payload.RequisitionId,
            Stage = "Hired",
            Value = 1m,
            OccurredAt = envelope.Payload.HiredAt,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }
}
