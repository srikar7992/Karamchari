using Karamchari.Billing.Contracts;
using Karamchari.TimeAttendance.Domain.Analytics;
using Karamchari.TimeAttendance.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Consumers;

/// <summary>
/// Closes the analytical loop by updating project-level revenue and cash flow metrics
/// whenever an invoice is issued or a payment is received.
///
/// Idempotency: Deduplicates on eventId + consumerName.
/// Logic: Attaches revenue/cash to the current day's grain.
/// </summary>
public sealed class BillingAnalyticsConsumer : 
    IConsumer<InvoiceIssuedIntegrationEvent>,
    IConsumer<PaymentReceivedIntegrationEvent>
{
    private const string ConsumerName = nameof(BillingAnalyticsConsumer);
    private readonly TimeAttendanceDbContext _db;

    public BillingAnalyticsConsumer(TimeAttendanceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<InvoiceIssuedIntegrationEvent> context)
    {
        var ev = context.Message;
        var date = DateOnly.FromDateTime(ev.OccurredAt.Date);

        // 1. Idempotency Gate
        if (await IsAlreadyProcessed(ev.EventId, context.CancellationToken)) return;

        // 2. Update Revenue (Invoiced)
        await UpsertProjectMetricAsync(ev.TenantId, ev.ProjectId, date, 
            revenueDelta: ev.TotalAmount, cashDelta: 0, 
            eventId: ev.EventId, occurredAt: ev.OccurredAt, context.CancellationToken);

        // 3. Mark Processed
        await MarkAsProcessed(ev.TenantId, ev.EventId, context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<PaymentReceivedIntegrationEvent> context)
    {
        var ev = context.Message;
        var date = DateOnly.FromDateTime(ev.OccurredAt.Date);

        if (await IsAlreadyProcessed(ev.EventId, context.CancellationToken)) return;

        // 2. Update Cash Flow (Collected)
        await UpsertProjectMetricAsync(ev.TenantId, ev.ProjectId, date, 
            revenueDelta: 0, cashDelta: ev.Amount, 
            eventId: ev.EventId, occurredAt: ev.OccurredAt, context.CancellationToken);

        await MarkAsProcessed(ev.TenantId, ev.EventId, context.CancellationToken);
    }

    private async Task UpsertProjectMetricAsync(
        string tenantId, Guid projectId, DateOnly date, 
        decimal revenueDelta, decimal cashDelta, 
        Guid eventId, DateTimeOffset occurredAt, CancellationToken ct)
    {
        var rows = await _db.Database.ExecuteSqlRawAsync(
            @"UPDATE Analytics_ProjectMetrics
              SET Revenue           = Revenue + {0},
                  CollectedAmount   = CollectedAmount + {1},
                  LastEventId       = {2},
                  LastProcessedOccurredAt = {3},
                  LastUpdatedAt     = {4}
              WHERE TenantId = {5} AND ProjectId = {6} AND Date = {7}
                AND LastEventId != {2}
                AND (LastProcessedOccurredAt IS NULL OR LastProcessedOccurredAt < {3})",
            revenueDelta, cashDelta, eventId, occurredAt, DateTimeOffset.UtcNow,
            tenantId, projectId, date, ct);

        if (rows == 0)
        {
            try
            {
                _db.ProjectMetrics.Add(new ProjectMetrics
                {
                    TenantId = tenantId,
                    ProjectId = projectId,
                    Date = date,
                    Revenue = revenueDelta,
                    CollectedAmount = cashDelta,
                    LastEventId = eventId,
                    LastProcessedOccurredAt = occurredAt,
                    LastUpdatedAt = DateTimeOffset.UtcNow
                });
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
            }
        }
    }

    private async Task<bool> IsAlreadyProcessed(Guid eventId, CancellationToken ct)
        => await _db.ProcessedEventLogs.AnyAsync(l => l.EventId == eventId && l.ConsumerName == ConsumerName, ct);

    private async Task MarkAsProcessed(string tenantId, Guid eventId, CancellationToken ct)
    {
        _db.ProcessedEventLogs.Add(new ProcessedEventLog
        {
            TenantId = tenantId,
            EventId = eventId,
            ConsumerName = ConsumerName,
            ProcessedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
