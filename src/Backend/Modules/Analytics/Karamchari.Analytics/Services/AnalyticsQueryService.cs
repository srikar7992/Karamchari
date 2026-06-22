using Karamchari.Analytics.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Analytics.Services;

public sealed class AnalyticsQueryService(AnalyticsDbContext db)
{
    public async Task<IReadOnlyList<HeadcountTrendPoint>> HeadcountTrendAsync(
        string tenantId, Guid? departmentId, int months, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMonths(-months);
        var q = db.AggMonthlyHeadcounts.AsNoTracking().Where(a => a.TenantId == tenantId);
        if (departmentId.HasValue) q = q.Where(a => a.DepartmentId == departmentId.Value);
        return await q
            .Where(a => new DateTime(a.Year, a.Month, 1) >= new DateTime(cutoff.Year, cutoff.Month, 1))
            .OrderBy(a => a.Year).ThenBy(a => a.Month)
            .Select(a => new HeadcountTrendPoint(a.Year, a.Month, a.HeadcountEnd, a.Hires, a.Attrition))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AttritionRatePoint>> AttritionRateAsync(
        string tenantId, int months, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMonths(-months);
        var facts = await db.FactAttrition.AsNoTracking()
            .Where(f => f.TenantId == tenantId && f.DateKey >= int.Parse(cutoff.ToString("yyyyMMdd")))
            .ToListAsync(ct);

        var agg = await db.AggMonthlyHeadcounts.AsNoTracking()
            .Where(a => a.TenantId == tenantId
                && new DateTime(a.Year, a.Month, 1) >= new DateTime(cutoff.Year, cutoff.Month, 1))
            .ToListAsync(ct);

        var result = from a in agg
                     let dateKey = a.Year * 10000 + a.Month * 100
                     let voluntary = facts.Count(f => f.DateKey / 100 * 100 == dateKey && f.TerminationType == "Voluntary")
                     let involuntary = facts.Count(f => f.DateKey / 100 * 100 == dateKey && f.TerminationType == "Involuntary")
                     let hce = a.HeadcountEnd > 0 ? (decimal)a.HeadcountEnd : 1m
                     select new AttritionRatePoint(a.Year, a.Month,
                         Math.Round(voluntary / hce * 100, 2),
                         Math.Round(involuntary / hce * 100, 2));
        return result.OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();
    }

    public async Task<IReadOnlyList<PayrollCostPoint>> PayrollCostTrendAsync(
        string tenantId, int months, CancellationToken ct)
    {
        var cutoff = int.Parse(DateTimeOffset.UtcNow.AddMonths(-months).ToString("yyyyMMdd"));
        return await db.FactWorkforceDaily.AsNoTracking()
            .Where(f => f.TenantId == tenantId && f.DateKey >= cutoff)
            .GroupBy(f => new { Year = f.DateKey / 10000, Month = f.DateKey / 100 % 100 })
            .Select(g => new PayrollCostPoint(g.Key.Year, g.Key.Month, g.Sum(x => x.TotalCTC), g.Average(x => x.TotalCTC)))
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(ct);
    }
}

public record HeadcountTrendPoint(int Year, int Month, int Headcount, int Hires, int Attrition);
public record AttritionRatePoint(int Year, int Month, decimal VoluntaryRate, decimal InvoluntaryRate);
public record PayrollCostPoint(int Year, int Month, decimal TotalCTC, decimal AvgCTC);

public sealed class WorkforceScorecardService(AnalyticsDbContext db)
{
    public async Task<WorkforceScorecard> GetScorecardAsync(string tenantId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var thisMonthKey = now.Year * 10000 + now.Month * 100;
        var last12MonthsCutoff = now.AddMonths(-12);

        var headcount = await db.AggMonthlyHeadcounts.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.Year == now.Year && a.Month == now.Month)
            .SumAsync(a => (int?)a.HeadcountEnd, ct) ?? 0;

        var totalHires12m = await db.AggMonthlyHeadcounts.AsNoTracking()
            .Where(a => a.TenantId == tenantId
                && new DateTime(a.Year, a.Month, 1) >= new DateTime(last12MonthsCutoff.Year, last12MonthsCutoff.Month, 1))
            .SumAsync(a => (int?)a.Hires, ct) ?? 0;

        var totalAttrition12m = await db.AggMonthlyHeadcounts.AsNoTracking()
            .Where(a => a.TenantId == tenantId
                && new DateTime(a.Year, a.Month, 1) >= new DateTime(last12MonthsCutoff.Year, last12MonthsCutoff.Month, 1))
            .SumAsync(a => (int?)a.Attrition, ct) ?? 0;

        var avgTenureMonths = await db.DimEmployees.AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.IsActive)
            .Select(e => (decimal)(DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - e.HireDate.DayNumber) / 30.44m)
            .DefaultIfEmpty(0)
            .AverageAsync(ct);

        var totalPayroll = await db.FactWorkforceDaily.AsNoTracking()
            .Where(f => f.TenantId == tenantId && f.DateKey >= thisMonthKey && f.IsActive)
            .SumAsync(f => (decimal?)f.TotalCTC, ct) ?? 0m;

        var attritionRate = headcount > 0 ? Math.Round((decimal)totalAttrition12m / headcount * 100, 2) : 0m;

        return new WorkforceScorecard(
            CurrentHeadcount: headcount,
            TotalHires12Months: totalHires12m,
            TotalAttrition12Months: totalAttrition12m,
            AttritionRatePercent: attritionRate,
            AverageTenureMonths: Math.Round(avgTenureMonths, 1),
            CurrentMonthPayroll: totalPayroll,
            AsOf: now);
    }

    public async Task<IReadOnlyList<CohortRetentionPoint>> GetCohortRetentionAsync(
        string tenantId, int cohortYear, CancellationToken ct)
    {
        var cohortStart = new DateOnly(cohortYear, 1, 1);
        var cohortEnd = new DateOnly(cohortYear, 12, 31);

        var hired = await db.DimEmployees.AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.HireDate >= cohortStart && e.HireDate <= cohortEnd)
            .Select(e => new { e.EmployeeId, e.HireDate, e.TerminationDate, e.IsActive })
            .ToListAsync(ct);

        if (hired.Count == 0) return [];

        var result = new List<CohortRetentionPoint>();
        for (int monthOffset = 0; monthOffset <= 12; monthOffset++)
        {
            var checkpoint = cohortStart.AddMonths(monthOffset);
            var retained = hired.Count(e => e.IsActive || (e.TerminationDate.HasValue && e.TerminationDate.Value > checkpoint));
            result.Add(new CohortRetentionPoint(monthOffset, retained, Math.Round((decimal)retained / hired.Count * 100, 1)));
        }
        return result;
    }

    public async Task<AttritionDecomposition> GetAttritionDecompositionAsync(
        string tenantId, int year, int month, CancellationToken ct)
    {
        var dateKeyMin = year * 10000 + month * 100;
        var dateKeyMax = dateKeyMin + 31;

        var facts = await db.FactAttrition.AsNoTracking()
            .Where(f => f.TenantId == tenantId && f.DateKey >= dateKeyMin && f.DateKey < dateKeyMax)
            .Select(f => new { f.TerminationType, f.TenureMonths, f.DepartmentId })
            .ToListAsync(ct);

        var byType = facts.GroupBy(f => f.TerminationType)
            .ToDictionary(g => g.Key, g => g.Count());

        var byDept = facts.GroupBy(f => f.DepartmentId)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        var earlyAttrition = facts.Count(f => f.TenureMonths < 12);

        return new AttritionDecomposition(year, month, facts.Count, byType, byDept, earlyAttrition);
    }
}

public record WorkforceScorecard(int CurrentHeadcount, int TotalHires12Months, int TotalAttrition12Months,
    decimal AttritionRatePercent, decimal AverageTenureMonths, decimal CurrentMonthPayroll, DateTimeOffset AsOf);
public record CohortRetentionPoint(int MonthOffset, int Retained, decimal RetentionPercent);
public record AttritionDecomposition(int Year, int Month, int Total,
    Dictionary<string, int> ByType, Dictionary<string, int> ByDepartment, int EarlyAttrition);
