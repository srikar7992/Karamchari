namespace Karamchari.PSA.Services;

using Karamchari.PSA.Domain;
using Karamchari.PSA.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Accounts Receivable aging and cash flow forecasting.
///
/// AR Aging buckets: Current, 1–30, 31–60, 60+
/// Cash flow forecast: weighted probability based on age of invoice.
/// </summary>
public sealed class CashFlowService
{
    private readonly PSADbContext _db;

    public CashFlowService(PSADbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Computes AR aging buckets from all unpaid invoices.
    /// </summary>
    public async Task<ARAgingReport> GetAgingAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var unpaid = await _db.Invoices
            .Where(i => i.IsFinalized)
            .ToListAsync(ct);

        decimal currentBucket = 0, bucket1to30 = 0, bucket31to60 = 0, bucket60plus = 0;

        foreach (var inv in unpaid)
        {
            var dueDate = inv.InvoiceDate.AddDays(30); // Net-30 terms
            var daysLate = today.DayNumber - dueDate.DayNumber;

            if (daysLate <= 0) currentBucket += inv.TotalAmount;
            else if (daysLate <= 30) bucket1to30 += inv.TotalAmount;
            else if (daysLate <= 60) bucket31to60 += inv.TotalAmount;
            else bucket60plus += inv.TotalAmount;
        }

        return new ARAgingReport(
            Current: currentBucket,
            Days1To30: bucket1to30,
            Days31To60: bucket31to60,
            Days60Plus: bucket60plus,
            TotalOutstanding: currentBucket + bucket1to30 + bucket31to60 + bucket60plus);
    }

    /// <summary>
    /// Forecasts expected cash inflow using weighted collection probability.
    /// Probability decreases as invoice ages:
    ///   Current: 90%, 1-30 late: 60%, 31-60 late: 30%, 60+: 10%.
    /// </summary>
    public async Task<CashFlowForecast> ForecastAsync(CancellationToken ct = default)
    {
        var aging = await GetAgingAsync(ct);

        decimal expected =
            aging.Current * 0.9m +
            aging.Days1To30 * 0.6m +
            aging.Days31To60 * 0.3m +
            aging.Days60Plus * 0.1m;

        // Revenue run rate: this month's revenue projected to end of month.
        var now = DateTime.UtcNow;
        var daysElapsed = now.Day;
        var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);

        var thisMonthRevenue = await _db.MonthlyMetrics
            .Where(m => m.Year == now.Year && m.Month == now.Month)
            .SumAsync(m => m.TotalRevenue, ct);

        decimal projectedMonthlyRevenue = daysElapsed > 0
            ? Math.Round(thisMonthRevenue / daysElapsed * daysInMonth, 2)
            : thisMonthRevenue;

        return new CashFlowForecast(
            ExpectedCollections: Math.Round(expected, 2),
            ProjectedMonthlyRevenue: projectedMonthlyRevenue,
            TotalOutstanding: aging.TotalOutstanding,
            CollectionProbability: aging.TotalOutstanding > 0
                ? Math.Round(expected / aging.TotalOutstanding * 100, 2)
                : 100);
    }
}

/// <summary>AR Aging buckets.</summary>
public record ARAgingReport(
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days60Plus,
    decimal TotalOutstanding);

/// <summary>Cash flow projection.</summary>
public record CashFlowForecast(
    decimal ExpectedCollections,
    decimal ProjectedMonthlyRevenue,
    decimal TotalOutstanding,
    decimal CollectionProbability);
