using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Performance.Domain.Goals;
using Karamchari.Performance.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Employee;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class EmployeeWorkspaceEndpoints
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapEmployeeWorkspace(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/me").RequireAuthorization();

        group.MapGet("/goals", GetMyGoals)
            .WithName("Me.Goals");

        group.MapGet("/reviews/inbox", GetMyReviewInbox)
            .WithName("Me.ReviewInbox");

        group.MapGet("/skills", GetMySkills)
            .WithName("Me.Skills");

        return app;
    }

    private static async Task<IResult> GetMyGoals(
        ClaimsPrincipal user,
        HttpRequest request,
        PerformanceDbContext perf,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();
        if (employeeId == Guid.Empty) return Results.Unauthorized();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize == 0 ? 20 : pageSize, 1, 100);

        var query = perf.Goals
            .AsNoTracking()
            .Where(g => g.OwnerId == employeeId && g.OwnerType == GoalOwnerType.Individual);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<GoalStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(g => g.Status == parsedStatus);
        }

        query = query.OrderByDescending(g => g.Weight).ThenBy(g => g.Title);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new MyGoalItem(
                g.Id,
                g.CycleId,
                g.Title,
                g.Description,
                g.Type.ToString(),
                g.TargetValue,
                g.Unit,
                g.CurrentProgress,
                g.FinalScore,
                g.Status.ToString(),
                g.Weight))
            .ToListAsync(ct);

        return Results.Ok(new MyGoalsResponse(items, total, page, pageSize, total > page * pageSize));
    }

    private static async Task<IResult> GetMyReviewInbox(
        ClaimsPrincipal user,
        HttpRequest request,
        PerformanceDbContext perf,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();
        if (employeeId == Guid.Empty) return Results.Unauthorized();

        var items = await perf.ReviewTaskInboxItems
            .AsNoTracking()
            .Where(i => i.ReviewerEmployeeId == employeeId)
            .OrderByDescending(i => i.Priority)
            .ThenBy(i => i.Deadline)
            .Select(i => new ReviewInboxItemDto(
                i.ReviewAssignmentId,
                i.RevieweeEmployeeId,
                i.RevieweeDisplayName,
                i.CycleName,
                i.ReviewerRole.ToString(),
                i.Deadline,
                i.IsOverdue,
                i.Priority))
            .ToListAsync(ct);

        return Results.Ok(new MyReviewsResponse(items, items.Count));
    }

    private static async Task<IResult> GetMySkills(
        ClaimsPrincipal user,
        HttpRequest request,
        PerformanceDbContext perf,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();
        if (employeeId == Guid.Empty) return Results.Unauthorized();

        var items = await perf.EmployeeSkillInventoryItems
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId)
            .OrderBy(s => s.SkillCategoryName)
            .ThenBy(s => s.SkillName)
            .Select(s => new SkillInventoryItemDto(
                s.SkillId,
                s.SkillName,
                s.SkillCategoryName,
                s.Domain.ToString(),
                s.CurrentProficiency.ToString(),
                s.TargetProficiency.HasValue ? s.TargetProficiency.Value.ToString() : null,
                s.ProficiencyGap,
                s.LastAssessedOnUtc))
            .ToListAsync(ct);

        var withGap = items.Count(s => s.ProficiencyGap.HasValue && s.ProficiencyGap.Value > 0);
        return Results.Ok(new MySkillsResponse(items, withGap));
    }
}
