using Karamchari.Analytics.Services;
using Karamchari.Core.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Karamchari.Api.BFF.Analytics;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/analytics/reports")
                   .RequireAuthorization(Permissions.AnalyticsRead);

        g.MapGet("/headcount-trend", HeadcountTrend);
        g.MapGet("/attrition-rate", AttritionRate);
        g.MapGet("/payroll-cost", PayrollCostTrend);

        // Sprint 11 — Analytics Maturity
        g.MapGet("/scorecard", GetScorecard);
        g.MapGet("/cohort-retention", GetCohortRetention);
        g.MapGet("/attrition-decomposition", GetAttritionDecomposition);

        // Platform Readiness — reconciliation surface
        app.MapGet("/api/v1/analytics/reconciliation", GetReconciliation)
           .RequireAuthorization(Permissions.AnalyticsRead);
    }

    private static async Task<IResult> HeadcountTrend(
        AnalyticsQueryService svc, HttpContext ctx, int months = 12, Guid? departmentId = null,
        System.Threading.CancellationToken ct = default)
        => Results.Ok(await svc.HeadcountTrendAsync(TenantId(ctx), departmentId, months, ct));

    private static async Task<IResult> AttritionRate(
        AnalyticsQueryService svc, HttpContext ctx, int months = 12,
        System.Threading.CancellationToken ct = default)
        => Results.Ok(await svc.AttritionRateAsync(TenantId(ctx), months, ct));

    private static async Task<IResult> PayrollCostTrend(
        AnalyticsQueryService svc, HttpContext ctx, int months = 12,
        System.Threading.CancellationToken ct = default)
        => Results.Ok(await svc.PayrollCostTrendAsync(TenantId(ctx), months, ct));

    private static async Task<IResult> GetScorecard(
        WorkforceScorecardService svc, HttpContext ctx,
        System.Threading.CancellationToken ct = default)
        => Results.Ok(await svc.GetScorecardAsync(TenantId(ctx), ct));

    private static async Task<IResult> GetCohortRetention(
        WorkforceScorecardService svc, HttpContext ctx,
        int cohortYear = 0, System.Threading.CancellationToken ct = default)
    {
        if (cohortYear == 0) cohortYear = DateTime.UtcNow.Year - 1;
        return Results.Ok(await svc.GetCohortRetentionAsync(TenantId(ctx), cohortYear, ct));
    }

    private static async Task<IResult> GetAttritionDecomposition(
        WorkforceScorecardService svc, HttpContext ctx,
        int year = 0, int month = 0, System.Threading.CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        if (year == 0) year = now.Year;
        if (month == 0) month = now.Month;
        return Results.Ok(await svc.GetAttritionDecompositionAsync(TenantId(ctx), year, month, ct));
    }

    private static async Task<IResult> GetReconciliation(
        AnalyticsReconciliationService svc, HttpContext ctx,
        System.Threading.CancellationToken ct = default)
    {
        var tenantId = TenantId(ctx);
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();
        return Results.Ok(await svc.ReconcileHeadcountAsync(tenantId, ct));
    }

    private static string TenantId(HttpContext ctx) =>
        ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
}
