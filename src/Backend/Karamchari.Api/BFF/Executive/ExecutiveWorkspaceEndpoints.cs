using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Performance.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Executive;

public static class ExecutiveWorkspaceEndpoints
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(30);

    public static WebApplication MapExecutiveWorkspace(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/executive").RequireAuthorization();

        group.MapGet("/performance/summary", GetOrgPerformanceSummary)
            .WithName("Executive.PerformanceSummary");

        group.MapGet("/talent/heatmap", GetOrgTalentHeatmap)
            .WithName("Executive.TalentHeatmap");

        return app;
    }

    private static async Task<IResult> GetOrgPerformanceSummary(
        PerformanceDbContext perf,
        [FromQuery] Guid? reviewCycleId,
        CancellationToken ct)
    {
        // Use the most-recent cycle if not specified
        var cycleId = reviewCycleId ?? await perf.ReviewCycles
            .AsNoTracking()
            .OrderByDescending(c => c.ReviewPeriodEnd)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(ct);

        if (cycleId == Guid.Empty) return Results.NotFound("No review cycles found.");

        var cycleName = await perf.ReviewCycles
            .AsNoTracking()
            .Where(c => c.Id == cycleId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var entries = await perf.TalentHeatmapEntries
            .AsNoTracking()
            .Where(e => e.ReviewCycleId == cycleId)
            .Select(e => new
            {
                e.NineBoxPosition,
                e.IsHighPerformer,
                e.IsAtRetentionRisk,
                e.IsPromotionReady,
                e.IsSuccessionCandidate,
                e.LastRefreshedUtc,
            })
            .ToListAsync(ct);

        if (entries.Count == 0) return Results.NotFound("No talent data for the specified cycle.");

        var now = DateTimeOffset.UtcNow;
        var dataAsOf = entries.Max(e => e.LastRefreshedUtc);

        var highPerformers = entries.Count(e => e.IsHighPerformer);
        var atRisk = entries.Count(e => e.IsAtRetentionRisk);
        var promotionReady = entries.Count(e => e.IsPromotionReady);
        var successionCandidates = entries.Count(e => e.IsSuccessionCandidate);
        var highPotentialHigh = entries.Count(e => e.NineBoxPosition == "High_High");

        var low = entries.Count(e => e.NineBoxPosition.StartsWith("Low", StringComparison.Ordinal));
        var medium = entries.Count - highPerformers - low;

        return Results.Ok(new OrgPerformanceSummaryResponse(
            cycleName,
            TotalEmployees: entries.Count,
            EvaluatedEmployees: entries.Count,
            EvaluationCoveragePercent: 100m,
            Distribution: new PerformanceDistributionDto(
                highPerformers, medium, low,
                entries.Count > 0 ? Math.Round((decimal)highPerformers / entries.Count * 100, 1) : 0),
            Succession: new SuccessionSummaryDto(successionCandidates, promotionReady, highPotentialHigh),
            RetentionRisk: new RetentionRiskSummaryDto(
                atRisk,
                entries.Count > 0 ? Math.Round((decimal)atRisk / entries.Count * 100, 1) : 0),
            DataAsOf: dataAsOf,
            IsStale: dataAsOf < now - StaleThreshold));
    }

    private static async Task<IResult> GetOrgTalentHeatmap(
        PerformanceDbContext perf,
        [FromQuery] Guid? reviewCycleId,
        [FromQuery] string? department,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);

        var cycleId = reviewCycleId ?? await perf.ReviewCycles
            .AsNoTracking()
            .OrderByDescending(c => c.ReviewPeriodEnd)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(ct);

        if (cycleId == Guid.Empty) return Results.NotFound("No review cycles found.");

        var query = perf.TalentHeatmapEntries
            .AsNoTracking()
            .Where(e => e.ReviewCycleId == cycleId);

        if (!string.IsNullOrWhiteSpace(department))
            query = query.Where(e => e.Department == department);

        query = query.OrderByDescending(e => e.CompositeScore).ThenBy(e => e.EmployeeDisplayName);

        var cycleName = await perf.ReviewCycles
            .AsNoTracking()
            .Where(c => c.Id == cycleId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var entries = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new OrgHeatmapEntry(
                e.EmployeeId,
                e.EmployeeDisplayName,
                e.Department,
                e.CareerLevel,
                e.NineBoxPosition,
                e.CompositeScore,
                e.IsHighPerformer,
                e.IsAtRetentionRisk,
                e.IsPromotionReady,
                e.IsSuccessionCandidate))
            .ToListAsync(ct);

        // Compute 9-box distribution from full dataset (not just this page)
        var allPositions = await perf.TalentHeatmapEntries
            .AsNoTracking()
            .Where(e => e.ReviewCycleId == cycleId)
            .Select(e => e.NineBoxPosition)
            .ToListAsync(ct);

        var distribution = new NineBoxDistributionDto(
            HighHigh: allPositions.Count(p => p == "High_High"),
            HighMedium: allPositions.Count(p => p == "High_Medium"),
            HighLow: allPositions.Count(p => p == "High_Low"),
            MediumHigh: allPositions.Count(p => p == "Medium_High"),
            MediumMedium: allPositions.Count(p => p == "Medium_Medium"),
            MediumLow: allPositions.Count(p => p == "Medium_Low"),
            LowHigh: allPositions.Count(p => p == "Low_High"),
            LowMedium: allPositions.Count(p => p == "Low_Medium"),
            LowLow: allPositions.Count(p => p == "Low_Low"));

        var dataAsOf = await perf.TalentHeatmapEntries
            .AsNoTracking()
            .Where(e => e.ReviewCycleId == cycleId)
            .MaxAsync(e => e.LastRefreshedUtc, ct);

        return Results.Ok(new OrgTalentHeatmapResponse(cycleName, entries, distribution, dataAsOf));
    }
}
