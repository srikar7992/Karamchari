using Karamchari.Billing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Billing.Services;

public sealed record ARSummary(
    decimal TotalOutstanding,
    decimal Overdue30,
    decimal Overdue60,
    decimal Overdue90,
    decimal OverduePlus,
    double DaysSalesOutstanding);

public sealed class ARAnalyticsService
{
    private readonly BillingDbContext _db;

    public ARAnalyticsService(BillingDbContext db) => _db = db;

    public async Task<ARSummary> GetSummaryAsync(string tenantId, CancellationToken ct = default)
    {
        var invoices = await _db.Invoices
            .Include(i => i.Payments)
            .Where(i => i.TenantId == tenantId && 
                        i.Status != Domain.Invoices.InvoiceStatus.Draft && 
                        i.Status != Domain.Invoices.InvoiceStatus.Cancelled)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        decimal o30 = 0, o60 = 0, o90 = 0, oPlus = 0, total = 0;

        foreach (var inv in invoices)
        {
            var paid = inv.Payments.Sum(p => p.Amount);
            var outstanding = inv.GrandTotal - paid;

            if (outstanding <= 0) continue;

            total += outstanding;

            // Days based on PeriodEnd or FinalizedAt
            var baseDate = inv.FinalizedAt?.Date != null 
                ? DateOnly.FromDateTime(inv.FinalizedAt.Value.Date) 
                : inv.PeriodEnd;
            
            var days = today.DayNumber - baseDate.DayNumber;

            if (days <= 30) o30 += outstanding;
            else if (days <= 60) o60 += outstanding;
            else if (days <= 90) o90 += outstanding;
            else oPlus += outstanding;
        }

        // DSO calculation (Simplified: Total AR / Average Daily Sales)
        // Average Daily Sales = (Sum of invoices in last 90 days) / 90
        var cutoff90 = today.AddDays(-90);
        var recentSales = invoices
            .Where(i => i.PeriodEnd >= cutoff90)
            .Sum(i => i.GrandTotal);
        
        double dso = recentSales > 0 ? (double)(total / (recentSales / 90)) : 0;

        return new ARSummary(total, o30, o60, o90, oPlus, dso);
    }
}
