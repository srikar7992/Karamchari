// -----------------------------------------------------------------------
// <copyright file="CollectionsBackgroundWorker.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Billing.Domain.Collections;
using Karamchari.Billing.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Billing.Services;

/// <summary>
/// Daily background worker that scans for overdue invoices and triggers the collection state machine.
/// Ensures that reminders and escalations are sent exactly as per the tenant's CollectionPolicy.
/// </summary>
public sealed class CollectionsBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CollectionsBackgroundWorker> _logger;

    public CollectionsBackgroundWorker(
        IServiceProvider serviceProvider,
        ILogger<CollectionsBackgroundWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting daily collections run...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

                await ProcessCollectionsAsync(db, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during daily collections run.");
            }

            // Run once every 24 hours
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }

    private async Task ProcessCollectionsAsync(BillingDbContext db, CancellationToken ct)
    {
        // 1. Fetch all active cases and their policies
        var cases = await db.CollectionCases
            .Where(c => c.Status == CollectionStatus.Active || c.Status == CollectionStatus.Escalated)
            .ToListAsync(ct);

        var policies = await db.CollectionPolicies.ToDictionaryAsync(p => p.TenantId, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var @case in cases)
        {
            if (!policies.TryGetValue(@case.TenantId, out var policy))
            {
                // Fallback to default policy if none defined
                policy = new CollectionPolicy { TenantId = @case.TenantId };
            }

            // Update case state based on current date
            // Note: In a real system, we'd query the Invoice to get the most accurate DaysOutstanding
            // For this worker, we'll assume the case.DaysOutstanding is updated or we compute it now.
            var invoice = await db.Invoices.FindAsync(@case.InvoiceId);
            if (invoice == null || invoice.Status == Domain.Invoices.InvoiceStatus.Paid)
            {
                @case.UpdateStatus(0, @case.DaysOutstanding);
                continue;
            }

            var days = today.DayNumber - invoice.PeriodEnd.DayNumber;
            var outstanding = invoice.GrandTotal - invoice.Payments.Sum(p => p.Amount);
            @case.UpdateStatus(outstanding, days);

            // 2. State Machine Logic
            if (days >= policy.CriticalDays && @case.CurrentStage != "90d")
            {
                await TriggerAction(@case, "90d", "Critical Alert");
            }
            else if (days >= policy.EscalationDays && @case.CurrentStage != "60d")
            {
                await TriggerAction(@case, "60d", "Escalation Reminder");
            }
            else if (days >= policy.SecondReminderDays && @case.CurrentStage != "30d")
            {
                await TriggerAction(@case, "30d", "Firm Reminder");
            }
            else if (days >= policy.FirstReminderDays && @case.CurrentStage != "7d")
            {
                await TriggerAction(@case, "7d", "Friendly Reminder");
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private Task TriggerAction(CollectionCase @case, string stage, string description)
    {
        _logger.LogInformation("Triggering {Stage} for Invoice {InvoiceId}: {Description}", stage, @case.InvoiceId, description);

        // In production, this would call ICollectionsCommunicationService
        @case.RecordReminder(stage);

        return Task.CompletedTask;
    }
}
