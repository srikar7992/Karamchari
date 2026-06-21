using Karamchari.Core.Messaging;
using Karamchari.Intelligence.Domain.Analytics;
using Karamchari.Intelligence.Persistence;
using Karamchari.Recruitment.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Intelligence.Consumers;

/// <summary>
/// Calculates the time-to-hire metric when a candidate is hired.
/// Looks back at the earliest "Published" velocity row for the same requisition to determine
/// how many days elapsed from requisition open to hire.
/// If no prior Published row exists (e.g. event ordering gap) the raw HiredAt is recorded
/// with Value = 0 so the record still lands and can be corrected during a replay.
/// </summary>
public sealed class TimeToHireConsumer(IntelligenceDbContext dbContext) :
    IConsumer<EnterpriseEventEnvelope<CandidateHiredIntegrationEvent>>
{
    private const string MetricType = "TimeToHire";

    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<CandidateHiredIntegrationEvent>> context)
    {
        var envelope = context.Message;
        var payload = envelope.Payload;

        // Find the earliest Published stage for this requisition to compute elapsed days
        var publishedRow = await dbContext.AnalyticsReadModels
            .Where(r =>
                r.TenantId == envelope.TenantId &&
                r.MetricType == "RecruitmentVelocity" &&
                r.EntityId == payload.RequisitionId &&
                r.Stage == "Published")
            .OrderBy(r => r.OccurredAt)
            .FirstOrDefaultAsync(context.CancellationToken);

        decimal daysToHire = 0m;
        if (publishedRow != null)
        {
            daysToHire = (decimal)(payload.HiredAt - publishedRow.OccurredAt).TotalDays;
        }

        dbContext.AnalyticsReadModels.Add(new AnalyticsReadModel
        {
            Id = Guid.NewGuid(),
            TenantId = envelope.TenantId,
            MetricType = MetricType,
            EntityId = payload.RequisitionId,
            Stage = "Hired",
            Value = daysToHire,
            OccurredAt = payload.HiredAt,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
