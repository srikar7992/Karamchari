// -----------------------------------------------------------------------
// <copyright file="StrategyEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.Api.BFF.Common;
using Karamchari.Intelligence.Domain.Signals;
using Karamchari.Intelligence.Persistence;
using Karamchari.Intelligence.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Intelligence;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class StrategyEndpoints
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapStrategyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/strategy").RequireAuthorization();

        // Organizational Health
        group.MapPost("/health/evaluate", EvaluateOrgHealth).WithName("Strategy.Health.Evaluate");
        group.MapGet("/health", ListHealthSignals).WithName("Strategy.Health.List");

        // Workforce Risk
        group.MapPost("/risk/detect", DetectRisks).WithName("Strategy.Risk.Detect");
        group.MapGet("/risk", ListRiskSignals).WithName("Strategy.Risk.List");

        // Executive Insights
        group.MapGet("/insights", ListInsights).WithName("Strategy.Insights.List");
        group.MapPost("/insights/{id:guid}/acknowledge", AcknowledgeInsight).WithName("Strategy.Insights.Acknowledge");

        // Scenarios
        group.MapPost("/scenarios", RunSimulation).WithName("Strategy.Scenarios.Run");
        group.MapGet("/scenarios", ListScenarios).WithName("Strategy.Scenarios.List");

        return app;
    }

    private static async Task<IResult> EvaluateOrgHealth(
        [FromBody] string orgUnitId,
        ClaimsPrincipal user,
        IOrganizationalHealthEngine engine)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var health = await engine.EvaluateHealthAsync(tenantId, orgUnitId);
        return Results.Ok(health);
    }

    private static async Task<IResult> ListHealthSignals(ClaimsPrincipal user, IntelligenceDbContext db, CancellationToken ct)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();
        var signals = await db.OrganizationalHealthSignals.AsNoTracking()
            .Where(s => s.TenantId == tenantId).ToListAsync(ct);
        return Results.Ok(signals);
    }

    private static async Task<IResult> DetectRisks(
        [FromBody] DetectRiskRequest request,
        ClaimsPrincipal user,
        IWorkforceRiskEngine engine)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var risk = await engine.DetectRisksAsync(tenantId, request.SubjectId, request.Category);
        return Results.Ok(risk);
    }

    private static async Task<IResult> ListRiskSignals(ClaimsPrincipal user, IntelligenceDbContext db, CancellationToken ct)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();
        var signals = await db.WorkforceRiskSignals.AsNoTracking()
            .Where(s => s.TenantId == tenantId).ToListAsync(ct);
        return Results.Ok(signals);
    }

    private static async Task<IResult> ListInsights(ClaimsPrincipal user, IntelligenceDbContext db, CancellationToken ct)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();
        var insights = await db.ExecutiveInsights.AsNoTracking()
            .Where(i => i.TenantId == tenantId).ToListAsync(ct);
        return Results.Ok(insights);
    }

    private static async Task<IResult> AcknowledgeInsight(Guid id, ClaimsPrincipal user, IntelligenceDbContext db)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();
        var insight = await db.ExecutiveInsights
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);
        if (insight == null) return Results.NotFound();

        insight.Acknowledge(user.Identity?.Name ?? "Admin");
        await db.SaveChangesAsync();
        return Results.Ok();
    }

    private static async Task<IResult> RunSimulation(
        [FromBody] RunSimulationRequest request,
        ClaimsPrincipal user,
        IWorkforcePlanningService service)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var scenario = await service.RunSimulationAsync(
            tenantId, request.Name, request.Description, request.ParametersJson, user.Identity?.Name ?? "Admin");

        return Results.Created($"/api/v1/strategy/scenarios/{scenario.Id}", scenario);
    }

    private static async Task<IResult> ListScenarios(ClaimsPrincipal user, IntelligenceDbContext db, CancellationToken ct)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();
        var scenarios = await db.StrategicWorkforceScenarios.AsNoTracking()
            .Where(s => s.TenantId == tenantId).ToListAsync(ct);
        return Results.Ok(scenarios);
    }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record DetectRiskRequest(string SubjectId, RiskCategory Category);
/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record RunSimulationRequest(string Name, string Description, string ParametersJson);
