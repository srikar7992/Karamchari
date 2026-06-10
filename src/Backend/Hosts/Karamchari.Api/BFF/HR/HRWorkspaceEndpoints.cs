// -----------------------------------------------------------------------
// <copyright file="HRWorkspaceEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Performance.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.HR;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class HRWorkspaceEndpoints
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapHRWorkspace(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/hr").RequireAuthorization();

        group.MapGet("/review-cycles", GetReviewCycles)
            .WithName("HR.ReviewCycles");

        group.MapGet("/calibration", GetCalibrationBoard)
            .WithName("HR.CalibrationBoard");

        group.MapGet("/promotions/pipeline", GetPromotionPipeline)
            .WithName("HR.PromotionPipeline");

        return app;
    }

    private static async Task<IResult> GetReviewCycles(
        PerformanceDbContext perf,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize == 0 ? 20 : pageSize, 1, 100);

        var query = perf.ReviewCycles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<Karamchari.Performance.Domain.Reviews.ReviewCycleStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(c => c.Status == parsedStatus);
        }

        query = query.OrderByDescending(c => c.ReviewPeriodEnd);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ReviewCycleDto(
                c.Id,
                c.Name,
                c.Type.ToString(),
                c.Status.ToString(),
                c.ReviewPeriodStart,
                c.ReviewPeriodEnd,
                c.SubmissionDeadline,
                c.TotalAssignments,
                c.CompletedAssignments,
                c.TotalAssignments > 0
                    ? Math.Round((decimal)c.CompletedAssignments / c.TotalAssignments * 100, 1)
                    : 0m))
            .ToListAsync(ct);

        return Results.Ok(new ReviewCyclePageResponse(items, total, page, pageSize, total > page * pageSize));
    }

    private static async Task<IResult> GetCalibrationBoard(
        PerformanceDbContext perf,
        [FromQuery] Guid? reviewCycleId,
        CancellationToken ct)
    {
        var query = perf.CalibrationBoardProjections.AsNoTracking();

        if (reviewCycleId.HasValue)
            query = query.Where(s => s.ReviewCycleId == reviewCycleId.Value);

        var sessions = await query
            .OrderBy(s => s.Status)
            .ThenByDescending(s => s.LastRefreshedUtc)
            .Select(s => new CalibrationSessionDto(
                s.CalibrationSessionId,
                s.ReviewCycleId,
                s.SessionName,
                s.Status.ToString(),
                s.TotalEntries,
                s.TopPerformerCount,
                s.HighPerformerCount,
                s.MeetsExpectationsCount,
                s.BelowExpectationsCount,
                s.UnderPerformerCount,
                s.TotalPanelMembers,
                s.PanelMembersJoined,
                s.AdjustmentsMade,
                s.EntriesPendingFinalization,
                s.FinalizedOnUtc,
                s.LastRefreshedUtc))
            .ToListAsync(ct);

        var pendingFinalization = sessions.Count(s => s.EntriesPendingFinalization > 0);
        return Results.Ok(new CalibrationBoardResponse(sessions, sessions.Count, pendingFinalization));
    }

    private static async Task<IResult> GetPromotionPipeline(
        PerformanceDbContext perf,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? stage,
        CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize == 0 ? 20 : pageSize, 1, 100);

        // HR sees all promotions, not filtered by visibility
        var baseQuery = perf.PromotionPipelineItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(stage) &&
            Enum.TryParse<Karamchari.Performance.Domain.Promotions.PromotionApprovalStage>(stage, ignoreCase: true, out var parsedStage))
        {
            baseQuery = baseQuery.Where(p => p.CurrentStage == parsedStage);
        }

        var query = baseQuery
            .OrderByDescending(p => p.ReadinessScore)
            .ThenBy(p => p.DaysInCurrentStage);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new HRPromotionEntry(
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

        return Results.Ok(new HRPromotionPipelineResponse(items, total, page, pageSize, total > page * pageSize));
    }
}
