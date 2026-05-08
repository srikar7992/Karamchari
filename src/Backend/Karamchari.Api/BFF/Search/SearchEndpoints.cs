using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Performance.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Search;

/// <summary>
/// Phase 1 search endpoints backed by SQL Server Full-Text Search (ADR-0014).
/// Phase 2: swap ISearchIndexer registration to AzureAiSearchIndexer — no endpoint changes.
/// </summary>
public static class SearchEndpoints
{
    private const int MaxQueryLength = 200;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public static WebApplication MapSearch(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/search").RequireAuthorization();

        group.MapGet("/employees", SearchEmployees).WithName("Search.Employees");
        group.MapGet("/talent", SearchTalent).WithName("Search.Talent");
        group.MapGet("/skills", SearchSkills).WithName("Search.Skills");

        return app;
    }

    private static async Task<IResult> SearchEmployees(
        [FromQuery] string? q,
        [FromQuery] string? dept,
        [FromQuery] string? level,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ClaimsPrincipal user,
        PerformanceDbContext db,
        CancellationToken ct)
    {
        var tenantId = user.GetTenantId();
        if (tenantId is null) return Results.Unauthorized();
        if (q is not null && q.Length > MaxQueryLength)
            return Results.BadRequest(new { error = "Query exceeds 200 character limit." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize == 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);

        // Phase 1: FTS on PromotionPipelineItems (EmployeeDisplayName + Department are FTS-indexed).
        // Phase 2: delegate to ISearchIndexer.SearchAsync<EmployeeDocument>.
        var query = db.PromotionPipelineItems
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(e =>
                EF.Functions.Contains(e.EmployeeDisplayName, q) ||
                EF.Functions.Contains(e.Department, q));

        if (!string.IsNullOrWhiteSpace(dept))
            query = query.Where(e => e.Department == dept);

        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(e => e.CurrentCareerLevel == level);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(e => e.EmployeeDisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.EmployeeId,
                e.EmployeeDisplayName,
                e.Department,
                Level = e.CurrentCareerLevel,
                ProposedLevel = e.ProposedCareerLevel,
                e.Status,
                e.ReadinessScore,
            })
            .ToListAsync(ct);

        return Results.Ok(new { total, page, pageSize, items });
    }

    private static async Task<IResult> SearchTalent(
        [FromQuery] string? q,
        [FromQuery] string? nineBoxPosition,
        [FromQuery] bool? isAtRetentionRisk,
        [FromQuery] Guid? cycleId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ClaimsPrincipal user,
        PerformanceDbContext db,
        CancellationToken ct)
    {
        var tenantId = user.GetTenantId();
        if (tenantId is null) return Results.Unauthorized();
        if (q is not null && q.Length > MaxQueryLength)
            return Results.BadRequest(new { error = "Query exceeds 200 character limit." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize == 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);

        var query = db.TalentHeatmapEntries
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(e =>
                EF.Functions.Contains(e.EmployeeDisplayName, q) ||
                EF.Functions.Contains(e.Department, q));

        if (!string.IsNullOrWhiteSpace(nineBoxPosition))
            query = query.Where(e => e.NineBoxPosition == nineBoxPosition);

        if (isAtRetentionRisk.HasValue)
            query = query.Where(e => e.IsAtRetentionRisk == isAtRetentionRisk.Value);

        if (cycleId.HasValue)
            query = query.Where(e => e.ReviewCycleId == cycleId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(e => e.EmployeeDisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.EmployeeId,
                e.EmployeeDisplayName,
                e.Department,
                e.CareerLevel,
                e.NineBoxPosition,
                e.IsAtRetentionRisk,
                e.IsHighPerformer,
                e.IsPromotionReady,
                e.IsSuccessionCandidate,
                e.CompositeScore,
                e.PotentialRating,
            })
            .ToListAsync(ct);

        return Results.Ok(new { total, page, pageSize, items });
    }

    private static async Task<IResult> SearchSkills(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] string? proficiency,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ClaimsPrincipal user,
        PerformanceDbContext db,
        CancellationToken ct)
    {
        var tenantId = user.GetTenantId();
        if (tenantId is null) return Results.Unauthorized();
        if (q is not null && q.Length > MaxQueryLength)
            return Results.BadRequest(new { error = "Query exceeds 200 character limit." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize == 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);

        var query = db.EmployeeSkillInventoryItems
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(e =>
                EF.Functions.Contains(e.SkillName, q) ||
                EF.Functions.Contains(e.SkillCategoryName, q));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.SkillCategoryName == category);

        // Proficiency filter: match enum name case-insensitively.
        if (!string.IsNullOrWhiteSpace(proficiency) &&
            Enum.TryParse<Karamchari.Performance.Domain.Skills.ProficiencyLevel>(
                proficiency, ignoreCase: true, out var profLevel))
            query = query.Where(e => e.CurrentProficiency == profLevel);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(e => e.SkillName)
            .ThenBy(e => e.EmployeeDisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.EmployeeId,
                e.EmployeeDisplayName,
                e.Department,
                e.SkillName,
                Category = e.SkillCategoryName,
                Proficiency = e.CurrentProficiency.ToString(),
                e.ProficiencyGap,
                e.LastAssessedOnUtc,
            })
            .ToListAsync(ct);

        return Results.Ok(new { total, page, pageSize, items });
    }
}
