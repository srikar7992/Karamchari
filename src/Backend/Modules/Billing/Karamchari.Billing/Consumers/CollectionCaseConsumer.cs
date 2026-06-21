// -----------------------------------------------------------------------
// <copyright file="CollectionCaseConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Billing.Contracts;
using Karamchari.Billing.Domain.Collections;
using Karamchari.Billing.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Billing.Consumers;

/// <summary>
/// Manages the lifecycle of CollectionCase entities based on financial events.
/// Creates cases for new invoices and closes them when payments are settled.
/// </summary>
public sealed class CollectionCaseConsumer :
    IConsumer<InvoiceIssuedIntegrationEventV1>,
    IConsumer<PaymentReceivedIntegrationEventV1>
{
    private const string ConsumerName = nameof(CollectionCaseConsumer);
    private readonly ILogger<CollectionCaseConsumer> _logger;
    private readonly BillingDbContext _db;

    public CollectionCaseConsumer(BillingDbContext db, ILogger<CollectionCaseConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task Consume(ConsumeContext<InvoiceIssuedIntegrationEventV1> context)
    {
        var ev = context.Message;
        _logger.LogInformation("Processing InvoiceIssued for {InvoiceId} [Tenant:{TenantId}]", ev.InvoiceId, ev.TenantId);

        using var tx = await _db.Database.BeginTransactionAsync(context.CancellationToken);
        try
        {
            if (await _db.ProcessedEventLogs.AnyAsync(l => l.EventId == ev.EventId && l.ConsumerName == ConsumerName, context.CancellationToken))
            {
                _logger.LogWarning("Duplicate InvoiceIssued event {EventId} ignored.", ev.EventId);
                return;
            }

            var @case = CollectionCase.Create(ev.TenantId, ev.InvoiceId, ev.TotalAmount + ev.TaxAmount, 0);
            _db.CollectionCases.Add(@case);

            _db.ProcessedEventLogs.Add(new Domain.Analytics.ProcessedEventLog
            {
                EventId = ev.EventId,
                ConsumerName = ConsumerName,
                TenantId = ev.TenantId
            });

            await _db.SaveChangesAsync(context.CancellationToken);
            await tx.CommitAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(context.CancellationToken);
            _logger.LogError(ex, "Failed to create collection case for Invoice:{InvoiceId}", ev.InvoiceId);
            throw;
        }
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task Consume(ConsumeContext<PaymentReceivedIntegrationEventV1> context)
    {
        var ev = context.Message;
        _logger.LogInformation("Processing PaymentReceived for {InvoiceId} [Tenant:{TenantId}]", ev.InvoiceId, ev.TenantId);

        using var tx = await _db.Database.BeginTransactionAsync(context.CancellationToken);
        try
        {
            if (await _db.ProcessedEventLogs.AnyAsync(l => l.EventId == ev.EventId && l.ConsumerName == ConsumerName, context.CancellationToken))
            {
                _logger.LogWarning("Duplicate PaymentReceived event {EventId} ignored.", ev.EventId);
                return;
            }

            var @case = await _db.CollectionCases
                .FirstOrDefaultAsync(c => c.TenantId == ev.TenantId && c.InvoiceId == ev.InvoiceId, context.CancellationToken);

            if (@case != null)
            {
                var invoice = await _db.Invoices
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.TenantId == ev.TenantId && i.Id == ev.InvoiceId, context.CancellationToken);

                if (invoice != null)
                {
                    var outstanding = invoice.GrandTotal - invoice.Payments.Sum(p => p.Amount);
                    @case.UpdateStatus(outstanding, @case.DaysOutstanding);
                    _logger.LogInformation("Updated CollectionCase:{CaseId} Status. Outstanding:{Amount}", @case.Id, outstanding);
                }
            }

            _db.ProcessedEventLogs.Add(new Domain.Analytics.ProcessedEventLog
            {
                EventId = ev.EventId,
                ConsumerName = ConsumerName,
                TenantId = ev.TenantId
            });

            await _db.SaveChangesAsync(context.CancellationToken);
            await tx.CommitAsync(context.CancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(context.CancellationToken);
            _logger.LogWarning("Concurrency conflict on CollectionCase for Invoice:{InvoiceId}. Retrying via MassTransit.", ev.InvoiceId);
            throw; // MassTransit will retry
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(context.CancellationToken);
            _logger.LogError(ex, "Failed to process payment for Invoice:{InvoiceId}", ev.InvoiceId);
            throw;
        }
    }
}
