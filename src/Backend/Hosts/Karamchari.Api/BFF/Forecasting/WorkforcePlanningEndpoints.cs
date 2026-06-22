using Karamchari.Core.Security;
using Karamchari.Forecasting.Domain;
using Karamchari.Forecasting.Persistence;
using Karamchari.Forecasting.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Forecasting;

public static class WorkforcePlanningEndpoints
{
    public static void MapWorkforcePlanningEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/workforce-planning").RequireAuthorization();

        g.MapGet("/scenarios", ListScenarios).RequireAuthorization(Permissions.WorkforcePlanningRead);
        g.MapPost("/scenarios", CreateScenario).RequireAuthorization(Permissions.WorkforcePlanningWrite);
        g.MapGet("/scenarios/{id:guid}", GetScenario).RequireAuthorization(Permissions.WorkforcePlanningRead);
        g.MapPost("/scenarios/{id:guid}/project", RunProjection).RequireAuthorization(Permissions.WorkforcePlanningWrite);
        g.MapGet("/scenarios/{id:guid}/variance", GetVariance).RequireAuthorization(Permissions.WorkforcePlanningRead);
        g.MapDelete("/scenarios/{id:guid}", ArchiveScenario).RequireAuthorization(Permissions.WorkforcePlanningWrite);
    }

    private static async Task<IResult> ListScenarios(
        HttpContext ctx, ForecastingDbContext db, CancellationToken ct)
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        var scenarios = await db.ForecastScenarios.AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .Select(s => new { s.Id, s.Name, Type = s.Type.ToString(), Status = s.Status.ToString(), s.CreatedAtUtc, s.LastProjectedAtUtc })
            .ToListAsync(ct);
        return Results.Ok(scenarios);
    }

    private static async Task<IResult> CreateScenario(
        [FromBody] CreateScenarioBody body, HttpContext ctx, ForecastingDbContext db, CancellationToken ct)
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var scenario = ForecastScenario.Create(tenantId, body.Name, body.Type);
        foreach (var a in body.Assumptions)
            scenario.AddAssumption(a);
        foreach (var p in body.HeadcountPlans)
            scenario.SetHeadcountPlan(p);

        db.ForecastScenarios.Add(scenario);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/workforce-planning/scenarios/{scenario.Id}", new { scenario.Id });
    }

    private static async Task<IResult> GetScenario(
        Guid id, HttpContext ctx, ForecastingDbContext db, CancellationToken ct)
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        var scenario = await db.ForecastScenarios.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        return scenario is null ? Results.NotFound() : Results.Ok(scenario);
    }

    private static async Task<IResult> RunProjection(
        Guid id, [FromBody] RunProjectionBody body, HttpContext ctx, ForecastingDbContext db, CancellationToken ct)
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        var scenario = await db.ForecastScenarios
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        if (scenario is null) return Results.NotFound();

        var projections = ScenarioModelingEngine.Project(
            body.StartingHeadcount, body.AvgMonthlySalary, scenario.Assumptions, body.HorizonMonths);
        scenario.ApplyProjections(projections);
        await db.SaveChangesAsync(ct);
        return Results.Ok(projections);
    }

    private static async Task<IResult> GetVariance(
        Guid id, HttpContext ctx, ForecastingDbContext db, CancellationToken ct)
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        var variances = await db.HeadcountVariances.AsNoTracking()
            .Where(v => v.ScenarioId == id && v.TenantId == tenantId)
            .OrderByDescending(v => v.ComputedAtUtc)
            .ToListAsync(ct);
        return Results.Ok(variances);
    }

    private static async Task<IResult> ArchiveScenario(
        Guid id, HttpContext ctx, ForecastingDbContext db, CancellationToken ct)
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        var scenario = await db.ForecastScenarios
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        if (scenario is null) return Results.NotFound();
        scenario.Archive();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private sealed record CreateScenarioBody(string Name, ScenarioType Type,
        List<ScenarioAssumption> Assumptions, List<HeadcountPlan> HeadcountPlans);
    private sealed record RunProjectionBody(int StartingHeadcount, decimal AvgMonthlySalary, int HorizonMonths = 36);
}
