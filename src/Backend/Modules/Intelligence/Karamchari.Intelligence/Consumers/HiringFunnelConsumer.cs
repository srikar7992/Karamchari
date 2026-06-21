using Karamchari.Core.Messaging;
using Karamchari.Intelligence.Domain.Analytics;
using Karamchari.Intelligence.Persistence;
using Karamchari.Recruitment.Contracts;
using MassTransit;

namespace Karamchari.Intelligence.Consumers;

/// <summary>
/// Records one funnel stage entry per event so that per-requisition conversion rates
/// can be queried by counting rows grouped by Stage.
/// EntityId is always the RequisitionId so all funnel rows for a requisition are co-located.
/// For events that only carry ApplicationId the ApplicationId is used as EntityId instead;
/// downstream queries join via separate velocity rows if needed.
/// </summary>
public sealed class HiringFunnelConsumer(IntelligenceDbContext dbContext) :
    IConsumer<EnterpriseEventEnvelope<CandidateAppliedIntegrationEventV1>>,
    IConsumer<EnterpriseEventEnvelope<InterviewCompletedIntegrationEventV1>>,
    IConsumer<EnterpriseEventEnvelope<OfferAcceptedIntegrationEventV1>>,
    IConsumer<EnterpriseEventEnvelope<CandidateHiredIntegrationEventV1>>
{
    private const string MetricType = "HiringFunnel";

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
        await dbContext.SaveChangesAsync(context.CancellationToken);
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
        await dbContext.SaveChangesAsync(context.CancellationToken);
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
        await dbContext.SaveChangesAsync(context.CancellationToken);
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
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
