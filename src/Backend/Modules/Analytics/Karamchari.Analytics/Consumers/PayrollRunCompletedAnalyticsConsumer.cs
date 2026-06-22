using Karamchari.Analytics.Persistence;
using Karamchari.Payroll.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Analytics.Consumers;

public sealed class PayrollRunCompletedAnalyticsConsumer(AnalyticsDbContext db)
    : IConsumer<StartPayrollRunCommand>
{
    // Fires when payroll period closes; updates salary data in most recent FactWorkforceDaily rows.
    public async Task Consume(ConsumeContext<StartPayrollRunCommand> context)
    {
        var msg = context.Message;
        var today = DateTimeOffset.UtcNow;
        var now = int.Parse(today.ToString("yyyyMMdd"));
        var monthStart = today.Year * 10000 + today.Month * 100 + 1;

        // Update AggMonthlyHeadcount for this period (upsert)
        var agg = await db.AggMonthlyHeadcounts
            .Where(a => a.TenantId == msg.TenantId && a.Year == today.Year && a.Month == today.Month)
            .ToListAsync(context.CancellationToken);

        // Recalculate headcount from FactWorkforceDaily
        var hcCounts = await db.FactWorkforceDaily
            .Where(f => f.TenantId == msg.TenantId && f.DateKey >= monthStart && f.DateKey <= now && f.IsActive)
            .GroupBy(f => f.DepartmentId)
            .Select(g => new { DeptId = g.Key, Count = g.Count() })
            .ToListAsync(context.CancellationToken);

        foreach (var hc in hcCounts)
        {
            var existing = agg.FirstOrDefault(a => a.DepartmentId == hc.DeptId);
            if (existing is null)
            {
                db.AggMonthlyHeadcounts.Add(new Domain.AggMonthlyHeadcount
                {
                    TenantId = msg.TenantId,
                    Year = today.Year,
                    Month = today.Month,
                    DepartmentId = hc.DeptId,
                    HeadcountEnd = hc.Count
                });
            }
            else
            {
                existing.HeadcountEnd = hc.Count;
            }
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
