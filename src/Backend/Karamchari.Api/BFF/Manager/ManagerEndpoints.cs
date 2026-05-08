using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.HR.Services;
using Karamchari.Performance.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Manager;

public static class ManagerEndpoints
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(15);

    public static WebApplication MapManagerWorkspace(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/manager").RequireAuthorization();

        group.MapGet("/dashboard", GetDashboard)
            .WithName("Manager.Dashboard");

        group.MapGet("/reviews/inbox", GetReviewInbox)
            .WithName("Manager.ReviewInbox");

        group.MapGet("/goals/team", GetTeamGoals)
            .WithName("Manager.TeamGoals");

        group.MapGet("/promotions/pipeline", GetPromotionPipeline)
            .WithName("Manager.PromotionPipeline");

        group.MapGet("/team/heatmap", GetTalentHeatmap)
            .WithName("Manager.TalentHeatmap");

        return app;
    }

    private static async Task<IResult> GetDashboard(
        ClaimsPrincipal user,
        HttpRequest request,
        PerformanceDbContext perf,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId(request);
        if (employeeId == Guid.Empty) return Results.Unauthorized();

        var now = DateTimeOffset.UtcNow;

        var projection = await perf.ManagerDashboardProjections
            .AsNoTracking()
            .Where(p => p.ManagerEmployeeId == employeeId)
            .OrderByDescending(p => p.LastRefreshedUtc)
            .Select(p => new ManagerDashboardResponse(
                p.CycleName,
                new GoalSummaryWidget(p.TotalTeamGoals, p.OnTrackGoals, p.OffTrackGoals, p.OverdueGoals, p.GoalCompletionRate),
                new ReviewSummaryWidget(p.TotalReviewsRequired, p.ReviewsCompleted, p.ReviewsOverdue, p.PendingApprovals),
                new KpiSummaryWidget(p.KPIsAtRisk, p.KPIsOffTrack),
                p.PendingPromotionApprovals,
                p.PendingFeedbackRequests,
                p.LastRefreshedUtc,
                p.LastRefreshedUtc < now - StaleThreshold))
            .FirstOrDefaultAsync(ct);

        return projection is null ? Results.NotFound() : Results.Ok(projection);
    }

    private static async Task<IResult> GetReviewInbox(
        ClaimsPrincipal user,
        HttpRequest request,
        PerformanceDbContext perf,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId(request);
        if (employeeId == Guid.Empty) return Results.Unauthorized();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize == 0 ? 20 : pageSize, 1, 100);

        var query = perf.ReviewTaskInboxItems
            .AsNoTracking()
            .Where(i => i.ReviewerEmployeeId == employeeId)
            .OrderByDescending(i => i.Priority)
            .ThenBy(i => i.Deadline);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new ReviewInboxItem(
                i.ReviewAssignmentId,
                i.RevieweeEmployeeId,
                i.RevieweeDisplayName,
                i.CycleName,
                i.ReviewerRole.ToString(),
                i.Deadline,
                i.IsOverdue,
                i.HoursUntilDeadline,
                i.Priority))
            .ToListAsync(ct);

        return Results.Ok(new ReviewInboxPageResponse(items, total, page, pageSize, total > page * pageSize));
    }

    private static async Task<IResult> GetTeamGoals(
        ClaimsPrincipal user,
        HttpRequest request,
        PerformanceDbContext perf,
        IVisibilityResolver visibility,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId(request);
        if (employeeId == Guid.Empty) return Results.Unauthorized();

        var now = DateTimeOffset.UtcNow;

        var summary = await perf.TeamGoalSummaries
            .AsNoTracking()
            .Where(s => s.ManagerEmployeeId == employeeId)
            .OrderByDescending(s => s.LastRefreshedUtc)
            .Select(s => new TeamGoalsResponse(
                s.CycleName,
                s.Department,
                s.CompletionRate,
                s.AverageProgress,
                s.TotalGoals,
                s.CompletedGoals,
                s.InProgressGoals,
                s.OffTrackGoals,
                s.NotStartedGoals,
                s.TeamSize,
                s.EmployeesWithNoGoals,
                s.LastRefreshedUtc,
                s.LastRefreshedUtc < now - StaleThreshold))
            .FirstOrDefaultAsync(ct);

        return summary is null ? Results.NotFound() : Results.Ok(summary);
    }

    private static async Task<IResult> GetPromotionPipeline(
        ClaimsPrincipal user,
        HttpRequest request,
        PerformanceDbContext perf,
        IVisibilityResolver visibility,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId(request);
        if (employeeId == Guid.Empty) return Results.Unauthorized();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize == 0 ? 20 : pageSize, 1, 100);

        var visibleIds = await visibility.ResolveVisibleEmployeeIdsAsync(
            employeeId, VisibilityScope.DirectAndFunctional, ct);

        var query = perf.PromotionPipelineItems
            .AsNoTracking()
            .Where(p => visibleIds.Contains(p.EmployeeId))
            .OrderByDescending(p => p.ReadinessScore)
            .ThenBy(p => p.DaysInCurrentStage);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PromotionPipelineEntry(
                p.PromotionRecommendationId,
                p.EmployeeId,
                p.EmployeeDisplayName,
                p.CurrentCareerLevel,
                p.ProposedCareerLevel,
                p.Department,
                p.Status.ToString(),
                p.CurrentStage.ToString(),
                p.ReadinessScore,
                p.DaysInCurrentStage,
                p.IsStale))
            .ToListAsync(ct);

        return Results.Ok(new PromotionPipelinePageResponse(items, total, page, pageSize, total > page * pageSize));
    }

    private static async Task<IResult> GetTalentHeatmap(
        ClaimsPrincipal user,
        HttpRequest request,
        PerformanceDbContext perf,
        IVisibilityResolver visibility,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId(request);
        if (employeeId == Guid.Empty) return Results.Unauthorized();

        var visibleIds = await visibility.ResolveVisibleEmployeeIdsAsync(
            employeeId, VisibilityScope.DirectAndFunctional, ct);

        var entries = await perf.TalentHeatmapEntries
            .AsNoTracking()
            .Where(e => visibleIds.Contains(e.EmployeeId))
            .OrderByDescending(e => e.LastRefreshedUtc)
            .ThenByDescending(e => e.CompositeScore)
            .Select(e => new TalentHeatmapItem(
                e.EmployeeId,
                e.EmployeeDisplayName,
                e.Department,
                e.CareerLevel,
                e.NineBoxPosition,
                e.CompositeScore,
                e.PotentialRating,
                e.IsHighPerformer,
                e.IsAtRetentionRisk,
                e.IsPromotionReady,
                e.IsSuccessionCandidate))
            .ToListAsync(ct);

        var dataAsOf = entries.Count > 0
            ? await perf.TalentHeatmapEntries.AsNoTracking()
                .Where(e => visibleIds.Contains(e.EmployeeId))
                .MaxAsync(e => e.LastRefreshedUtc, ct)
            : DateTimeOffset.UtcNow;

        var cycleName = await perf.TalentHeatmapEntries.AsNoTracking()
            .Where(e => visibleIds.Contains(e.EmployeeId))
            .OrderByDescending(e => e.LastRefreshedUtc)
            .Select(e => e.CycleName)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        return Results.Ok(new TalentHeatmapResponse(cycleName, entries, dataAsOf));
    }
}
