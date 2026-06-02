using Karamchari.Core.Messaging;
using Karamchari.Recruitment.Application;
using Karamchari.Recruitment.Contracts;
using MassTransit;

namespace Karamchari.Recruitment.Worker.Consumers;

public sealed class RequisitionPublishedAuditConsumer(IRecruitmentDbContext dbContext)
    : IConsumer<EnterpriseEventEnvelope<RequisitionPublishedIntegrationEvent>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<RequisitionPublishedIntegrationEvent>> context)
    {
        var envelope = context.Message;
        var payload = envelope.Payload;

        var auditEntry = new RecruitmentAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityId = payload.RequisitionId,
            EntityType = "JobRequisition",
            Action = "Published",
            NewState = System.Text.Json.JsonSerializer.Serialize(payload),
            Timestamp = envelope.OccurredAtUtc,
            UserId = envelope.Producer,
            CorrelationId = envelope.CorrelationId != null ? Guid.Parse(envelope.CorrelationId) : null,
            TenantId = envelope.TenantId
        };

        dbContext.AuditStream.Add(auditEntry);
        await dbContext.SaveChangesAsync();
    }
}
