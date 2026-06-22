using Karamchari.Analytics.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Analytics.Services;

public sealed record ReconciliationResult(
    string TenantId,
    int AnalyticsActiveEmployees,
    int AnalyticsTotalHiresYtd,
    int AnalyticsTotalAttritionYtd,
    DateTimeOffset AsOfUtc);

public sealed class AnalyticsReconciliationService(AnalyticsDbContext db)
{
    public async Task<ReconciliationResult> ReconcileHeadcountAsync(string tenantId, CancellationToken ct)
    {
        var activeEmployees = await db.DimEmployees
            .IgnoreQueryFilters()
            .CountAsync(e => e.TenantId == tenantId && e.IsActive, ct);

        var yearStart = DateTime.UtcNow.Year * 10000 + 101; // yyyyMMdd for Jan 1

        var totalHires = await db.FactHiring
            .IgnoreQueryFilters()
            .CountAsync(f => f.TenantId == tenantId && f.DateKey >= yearStart, ct);

        var totalAttrition = await db.FactAttrition
            .IgnoreQueryFilters()
            .CountAsync(f => f.TenantId == tenantId && f.DateKey >= yearStart, ct);

        return new ReconciliationResult(tenantId, activeEmployees, totalHires, totalAttrition, DateTimeOffset.UtcNow);
    }
}
