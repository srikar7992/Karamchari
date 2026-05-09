using Karamchari.Billing.Contracts;
using Karamchari.Forecasting.Domain;
using Karamchari.Forecasting.Persistence;
using Karamchari.TimeAttendance.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Forecasting.Consumers;

/// <summary>
/// Synchronizes forecasting metrics with real-time financial events.
/// Implements behavioral learning by updating client payment profiles on each collection.
/// </summary>
public sealed class ForecastUpdateConsumer :
    IConsumer<TimesheetApprovedIntegrationEvent>,
    IConsumer<InvoiceIssuedIntegrationEvent>,
    IConsumer<PaymentReceivedIntegrationEvent>
{
    private readonly ForecastingDbContext _db;
    private readonly Karamchari.Billing.Persistence.BillingDbContext _billingDb;

    public ForecastUpdateConsumer(
        ForecastingDbContext db,
        Karamchari.Billing.Persistence.BillingDbContext billingDb)
    {
        _db = db;
        _billingDb = billingDb;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task Consume(ConsumeContext<TimesheetApprovedIntegrationEvent> context)
    {
        var ev = context.Message;
        var date = DateOnly.FromDateTime(ev.OccurredAt.Date);

        var metric = await GetOrCreateMetric(ev.TenantId, date);

        decimal totalProjectedValue = 0;

        foreach (var entry in ev.Entries.Where(e => e.IsBillable && e.ProjectId.HasValue))
        {
            var projectId = entry.ProjectId!.Value;

            // Resolve Role
            var roleMapping = await _billingDb.EmployeeRoles
                .Where(r => r.TenantId == ev.TenantId && r.EmployeeId == ev.EmployeeId &&
                            (r.ProjectId == null || r.ProjectId == projectId) &&
                            r.EffectiveFrom <= entry.Date && (r.EffectiveTo == null || r.EffectiveTo >= entry.Date))
                .OrderByDescending(r => r.ProjectId)
                .FirstOrDefaultAsync(context.CancellationToken);

            if (roleMapping == null) continue;

            // Resolve Contract and Rate
            var contract = await _billingDb.Contracts
                .Include(c => c.RateCards)
                .Where(c => c.TenantId == ev.TenantId && c.ProjectId == projectId && c.IsActive)
                .FirstOrDefaultAsync(context.CancellationToken);

            if (contract == null) continue;

            try
            {
                var rate = contract.ResolveRate(roleMapping.RoleId, entry.Date);
                totalProjectedValue += entry.Hours * rate;
            }
            catch (InvalidOperationException) { continue; }
        }

        metric.ProjectedRevenue += totalProjectedValue;
        metric.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task Consume(ConsumeContext<InvoiceIssuedIntegrationEvent> context)
    {
        var ev = context.Message;
        var date = DateOnly.FromDateTime(ev.OccurredAt.Date);

        var metric = await GetOrCreateMetric(ev.TenantId, date);

        // Finalized invoice moves revenue from "Projected" to "Outstanding"
        metric.ProjectedRevenue -= ev.TotalAmount;
        metric.OutstandingAmount += ev.TotalAmount + ev.TaxAmount;

        // Cash projection is probability-weighted (Initial 95%)
        metric.ProjectedCash += (ev.TotalAmount + ev.TaxAmount) * 0.95m;

        metric.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task Consume(ConsumeContext<PaymentReceivedIntegrationEvent> context)
    {
        var ev = context.Message;
        var date = DateOnly.FromDateTime(ev.OccurredAt.Date);

        var metric = await GetOrCreateMetric(ev.TenantId, date);

        // Payment reduces outstanding and projected cash
        metric.OutstandingAmount -= ev.Amount;
        metric.ProjectedCash -= ev.Amount * 0.95m; // Reversing the initial projection

        metric.LastUpdatedAt = DateTimeOffset.UtcNow;

        // LEARNING: Update Client Payment Profile
        await UpdateClientProfile(ev.TenantId, ev.InvoiceId, ev.Amount, ev.OccurredAt);

        await _db.SaveChangesAsync();
    }

    private async Task<ForecastMetrics> GetOrCreateMetric(string tenantId, DateOnly date)
    {
        var metric = await _db.ForecastMetrics
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Date == date);

        if (metric == null)
        {
            metric = new ForecastMetrics { TenantId = tenantId, Date = date };
            _db.ForecastMetrics.Add(metric);
        }

        return metric;
    }

    private async Task UpdateClientProfile(string tenantId, Guid invoiceId, decimal amount, DateTimeOffset paidAt)
    {
        var invoice = await _billingDb.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null || invoice.FinalizedAt == null) return;

        var profile = await _db.ClientPaymentProfiles
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.ClientId == invoice.ClientId);

        if (profile == null)
        {
            profile = new ClientPaymentProfile { TenantId = tenantId, ClientId = invoice.ClientId };
            _db.ClientPaymentProfiles.Add(profile);
        }

        // Calculate days taken to pay this installment
        var daysToPay = (paidAt - invoice.FinalizedAt.Value).Days;

        // Update Running Average: (OldAvg * Count + NewVal) / (Count + 1)
        profile.AverageDaysToPay = (profile.AverageDaysToPay * profile.TotalInvoicesPaid + daysToPay) / (profile.TotalInvoicesPaid + 1);

        // Update On-Time Rate (if paid within 30 days of period end)
        bool isOnTime = daysToPay <= 30;
        decimal totalOnTime = (profile.OnTimeRate / 100m) * profile.TotalInvoicesPaid;
        if (isOnTime) totalOnTime++;

        profile.TotalInvoicesPaid++;
        profile.OnTimeRate = (totalOnTime / profile.TotalInvoicesPaid) * 100m;

        profile.LastUpdatedAt = DateTimeOffset.UtcNow;
    }
}
