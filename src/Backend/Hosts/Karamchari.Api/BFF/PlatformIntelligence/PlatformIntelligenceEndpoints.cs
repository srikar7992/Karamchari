// -----------------------------------------------------------------------
// <copyright file="PlatformIntelligenceEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.PlatformIntelligence.Persistence;
using Karamchari.PlatformIntelligence.Services;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.PlatformIntelligence;

public static class PlatformIntelligenceEndpoints
{
    public static WebApplication MapPlatformIntelligenceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/platform-intelligence").RequireAuthorization();

        group.MapGet("/decision", GetPlatformDecision)
            .WithName("PlatformIntelligence.Decision.Get");
        group.MapPost("/decision/generate", GeneratePlatformDecision)
            .WithName("PlatformIntelligence.Decision.Generate");

        group.MapGet("/scenarios", GetScenarios)
            .WithName("PlatformIntelligence.Scenarios.List");
        group.MapPost("/scenarios", CreateScenario)
            .WithName("PlatformIntelligence.Scenarios.Create");
        group.MapPost("/scenarios/{id}/evaluate", EvaluateScenario)
            .WithName("PlatformIntelligence.Scenarios.Evaluate");
        group.MapGet("/scenarios/optimal", GetOptimalScenario)
            .WithName("PlatformIntelligence.Scenarios.Optimal");
        group.MapPost("/scenarios/find-optimal", FindOptimalScenario)
            .WithName("PlatformIntelligence.Scenarios.FindOptimal");

        group.MapPost("/simulation/run", RunSimulation)
            .WithName("PlatformIntelligence.Simulation.Run");
        group.MapGet("/simulation", GetSimulations)
            .WithName("PlatformIntelligence.Simulation.List");

        group.MapPost("/optimization/run", RunOptimization)
            .WithName("PlatformIntelligence.Optimization.Run");
        group.MapGet("/optimization/latest", GetLatestOptimization)
            .WithName("PlatformIntelligence.Optimization.Latest");

        group.MapGet("/executive-digest", GetExecutiveDigest)
            .WithName("PlatformIntelligence.ExecutiveDigest.Get");
        group.MapPost("/executive-digest/generate", GenerateExecutiveDigest)
            .WithName("PlatformIntelligence.ExecutiveDigest.Generate");

        group.MapGet("/risks", GetRisks)
            .WithName("PlatformIntelligence.Risks.Get");
        group.MapPost("/risks/evaluate", EvaluateRisks)
            .WithName("PlatformIntelligence.Risks.Evaluate");

        group.MapGet("/recommendations", GetRecommendations)
            .WithName("PlatformIntelligence.Recommendations.List");
        group.MapPost("/recommendations/generate", GenerateRecommendations)
            .WithName("PlatformIntelligence.Recommendations.Generate");
        group.MapPost("/recommendations/{id}/acknowledge", AcknowledgeRecommendation)
            .WithName("PlatformIntelligence.Recommendations.Acknowledge");
        group.MapPost("/recommendations/{id}/dismiss", DismissRecommendation)
            .WithName("PlatformIntelligence.Recommendations.Dismiss");

        return app;
    }

    private static async Task<IResult> GetPlatformDecision(
        ClaimsPrincipal user,
        PlatformIntelligenceDbContext db,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var decision = await db.PlatformDecisions
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.GeneratedAt)
            .FirstOrDefaultAsync(ct);

        return decision is null ? Results.NotFound() : Results.Ok(decision);
    }

    private static async Task<IResult> GeneratePlatformDecision(
        ClaimsPrincipal user,
        PlatformDecisionService svc,
        string scopeKey = "tenant",
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var decision = await svc.GenerateDecisionAsync(tenantId, scopeKey, ct);
        return Results.Created($"/api/v1/platform-intelligence/decision", decision);
    }

    private static async Task<IResult> GetScenarios(
        ClaimsPrincipal user,
        PlatformIntelligenceDbContext db,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var scenarios = await db.WorkforceScenarios
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        return Results.Ok(scenarios);
    }

    private static async Task<IResult> CreateScenario(
        ClaimsPrincipal user,
        ScenarioEvaluationService svc,
        CreateScenarioRequest body,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var scenario = await svc.CreateScenarioAsync(tenantId, body.Name, body.Description, body.ConstraintsJson, body.CreatedBy, ct);
        return Results.Created($"/api/v1/platform-intelligence/scenarios/{scenario.Id}", scenario);
    }

    private static async Task<IResult> EvaluateScenario(
        ClaimsPrincipal user,
        ScenarioEvaluationService svc,
        Guid id,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var result = await svc.EvaluateScenarioAsync(id, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetOptimalScenario(
        ClaimsPrincipal user,
        PlatformIntelligenceDbContext db,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var scenario = await db.WorkforceScenarios
            .Where(s => s.TenantId == tenantId && s.IsOptimal)
            .FirstOrDefaultAsync(ct);
        return scenario is null ? Results.NotFound() : Results.Ok(scenario);
    }

    private static async Task<IResult> FindOptimalScenario(
        ClaimsPrincipal user,
        ScenarioEvaluationService svc,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var scenario = await svc.FindOptimalScenarioAsync(tenantId, ct);
        return Results.Created($"/api/v1/platform-intelligence/scenarios/optimal", scenario);
    }

    private static async Task<IResult> RunSimulation(
        ClaimsPrincipal user,
        WorkforceSimulationService svc,
        RunSimulationRequest body,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var simulation = await svc.RunAsync(tenantId, body.Name, body.ParametersJson, body.RequestedBy, ct);
        return Results.Created($"/api/v1/platform-intelligence/simulation", simulation);
    }

    private static async Task<IResult> GetSimulations(
        ClaimsPrincipal user,
        PlatformIntelligenceDbContext db,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var simulations = await db.SimulationRuns
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        return Results.Ok(simulations);
    }

    private static async Task<IResult> RunOptimization(
        ClaimsPrincipal user,
        WorkforceOptimizationService svc,
        OptimizeRequest body,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var result = await svc.OptimizeAsync(tenantId, body.ObjectiveType, body.ConstraintsJson, ct);
        return Results.Created($"/api/v1/platform-intelligence/optimization/latest", result);
    }

    private static async Task<IResult> GetLatestOptimization(
        ClaimsPrincipal user,
        PlatformIntelligenceDbContext db,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var optimization = await db.WorkforceOptimizations
            .Where(o => o.TenantId == tenantId)
            .OrderByDescending(o => o.GeneratedAt)
            .FirstOrDefaultAsync(ct);

        return optimization is null ? Results.NotFound() : Results.Ok(optimization);
    }

    private static async Task<IResult> GetExecutiveDigest(
        ClaimsPrincipal user,
        ExecutiveDigestService svc,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var digest = await svc.GetLatestDigestAsync(tenantId, ct);
        return digest is null ? Results.NotFound() : Results.Ok(digest);
    }

    private static async Task<IResult> GenerateExecutiveDigest(
        ClaimsPrincipal user,
        ExecutiveDigestService svc,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var digest = await svc.GenerateDailyDigestAsync(tenantId, ct);
        return Results.Created($"/api/v1/platform-intelligence/executive-digest", digest);
    }

    private static async Task<IResult> GetRisks(
        ClaimsPrincipal user,
        WorkforceRiskService svc,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var risks = await svc.GetCurrentRisksAsync(tenantId, ct);
        return Results.Ok(risks);
    }

    private static async Task<IResult> EvaluateRisks(
        ClaimsPrincipal user,
        WorkforceRiskService svc,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var risks = await svc.EvaluateRisksAsync(tenantId, ct);
        return Results.Ok(risks);
    }

    private static async Task<IResult> GetRecommendations(
        ClaimsPrincipal user,
        RecommendationService svc,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var recs = await svc.GetActiveRecommendationsAsync(tenantId, ct);
        return Results.Ok(recs);
    }

    private static async Task<IResult> GenerateRecommendations(
        ClaimsPrincipal user,
        RecommendationService svc,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var recs = await svc.GenerateRecommendationsAsync(tenantId, ct);
        return Results.Created("/api/v1/platform-intelligence/recommendations", recs);
    }

    private static async Task<IResult> AcknowledgeRecommendation(
        ClaimsPrincipal user,
        RecommendationService svc,
        Guid id,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var rec = await svc.AcknowledgeAsync(id, ct);
        return rec is null ? Results.NotFound() : Results.Ok(rec);
    }

    private static async Task<IResult> DismissRecommendation(
        ClaimsPrincipal user,
        RecommendationService svc,
        Guid id,
        CancellationToken ct = default)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var rec = await svc.DismissAsync(id, ct);
        return rec is null ? Results.NotFound() : Results.Ok(rec);
    }
}

sealed record CreateScenarioRequest(string Name, string Description, string ConstraintsJson, string CreatedBy);
sealed record RunSimulationRequest(string Name, string ParametersJson, string RequestedBy);
sealed record OptimizeRequest(string ScopeKey, string ObjectiveType, string ConstraintsJson);
