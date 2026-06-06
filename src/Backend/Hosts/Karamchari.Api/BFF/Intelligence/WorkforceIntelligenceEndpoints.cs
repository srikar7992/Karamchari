using System.Security.Claims;
using Karamchari.Api.BFF.Common;
using Karamchari.Intelligence.Services;

namespace Karamchari.Api.BFF.Intelligence;

/// <summary>
/// Executive workforce intelligence dashboard endpoints.
/// All endpoints are read-only; data is computed nightly by <c>WorkforceIntelligenceRecomputeJob</c>.
/// </summary>
public static class WorkforceIntelligenceEndpoints
{
    /// <summary>Registers all workforce intelligence endpoints.</summary>
    public static WebApplication MapWorkforceIntelligenceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/workforce-intelligence").RequireAuthorization();

        group.MapGet("/hotspots", GetHotspots)
            .WithName("WorkforceIntelligence.Hotspots");

        group.MapGet("/forecasts/at-risk", GetHighRiskForecasts)
            .WithName("WorkforceIntelligence.Forecasts.AtRisk");

        group.MapGet("/dependency-risks", GetDependencyRisks)
            .WithName("WorkforceIntelligence.DependencyRisks");

        group.MapGet("/coverage-fragility", GetCoverageFragility)
            .WithName("WorkforceIntelligence.CoverageFragility");

        group.MapGet("/recommendations/summary", GetRecommendationSummary)
            .WithName("WorkforceIntelligence.Recommendations.Summary");

        group.MapGet("/causal-chain-alerts", GetCausalChainAlerts)
            .WithName("WorkforceIntelligence.CausalChainAlerts");

        return app;
    }

    // GET /api/v1/workforce-intelligence/hotspots?scope=site
    private static async Task<IResult> GetHotspots(
        ClaimsPrincipal user,
        WorkforceDashboardQueryService svc,
        string? scope = null,
        CancellationToken ct = default)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var hotspots = await svc.GetHotspotsAsync(tenantId, scope, ct);
        return Results.Ok(hotspots);
    }

    // GET /api/v1/workforce-intelligence/forecasts/at-risk
    private static async Task<IResult> GetHighRiskForecasts(
        ClaimsPrincipal user,
        WorkforceDashboardQueryService svc,
        CancellationToken ct = default)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var forecasts = await svc.GetHighRiskForecastsAsync(tenantId, ct);
        return Results.Ok(forecasts);
    }

    // GET /api/v1/workforce-intelligence/dependency-risks?top=20
    private static async Task<IResult> GetDependencyRisks(
        ClaimsPrincipal user,
        WorkforceDashboardQueryService svc,
        int top = 20,
        CancellationToken ct = default)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var risks = await svc.GetTopDependencyRisksAsync(tenantId, top, ct);
        return Results.Ok(risks);
    }

    // GET /api/v1/workforce-intelligence/coverage-fragility?scope=site
    private static async Task<IResult> GetCoverageFragility(
        ClaimsPrincipal user,
        WorkforceDashboardQueryService svc,
        string? scope = null,
        CancellationToken ct = default)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var fragility = await svc.GetCoverageFragilityAsync(tenantId, scope, ct);
        return Results.Ok(fragility);
    }

    // GET /api/v1/workforce-intelligence/recommendations/summary
    private static async Task<IResult> GetRecommendationSummary(
        ClaimsPrincipal user,
        WorkforceDashboardQueryService svc,
        CancellationToken ct = default)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var summary = await svc.GetRecommendationSummaryAsync(tenantId, ct);
        return Results.Ok(summary);
    }

    // GET /api/v1/workforce-intelligence/causal-chain-alerts
    private static async Task<IResult> GetCausalChainAlerts(
        ClaimsPrincipal user,
        WorkforceDashboardQueryService svc,
        CancellationToken ct = default)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var alerts = await svc.GetCausalChainAlertsAsync(tenantId, ct);
        return Results.Ok(alerts);
    }
}
