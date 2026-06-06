using System.Security.Claims;
using Karamchari.Compliance.Domain.LegalHold;
using Karamchari.Compliance.Domain.Violations;
using Karamchari.Compliance.Services;

namespace Karamchari.Api.BFF.Compliance;

/// <summary>
/// Phase 7 — Compliance &amp; Governance Platform endpoints.
/// Read-only dashboard, violation management, legal holds, regulatory registers, audit packages.
/// </summary>
public static class GovernanceEndpoints
{
    public static WebApplication MapGovernanceEndpoints(this WebApplication app)
    {
        var gov = app.MapGroup("/api/governance").RequireAuthorization();

        // Dashboard
        gov.MapGet("/dashboard", GetDashboard);
        gov.MapGet("/score", GetComplianceScore);

        // Violations
        gov.MapGet("/violations", GetOpenViolations);
        gov.MapPut("/violations/{id}/resolve", ResolveViolation);
        gov.MapPut("/violations/{id}/accept-risk", AcceptViolationRisk);
        gov.MapPut("/violations/{id}/investigate", StartViolationInvestigation);

        // Legal Holds
        gov.MapGet("/legal-holds", GetActiveLegalHolds);
        gov.MapPost("/legal-holds", CreateLegalHold);
        gov.MapPut("/legal-holds/{id}/release", ReleaseLegalHold);

        // Audit Packages
        gov.MapPost("/audit-packages", RequestAuditPackage);
        gov.MapPost("/audit-packages/{id}/generate", GenerateAuditPackage);

        // Scoring recompute (admin/nightly trigger)
        gov.MapPost("/score/recompute", RecomputeScore);

        // Analytics
        gov.MapGet("/analytics/trend", GetScoreTrend);
        gov.MapGet("/analytics/velocity", GetViolationVelocity);
        gov.MapGet("/analytics/heatmap", GetRiskHeatmap);

        return app;
    }

    private static async Task<IResult> GetDashboard(
        ComplianceDashboardQueryService dashboard,
        ClaimsPrincipal user)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        var score = await dashboard.GetTenantScoreAsync(tenantId);
        var violations = await dashboard.GetViolationTrendAsync(tenantId);
        var holds = await dashboard.GetActiveLegalHoldsAsync(tenantId);
        var retention = await dashboard.GetRetentionQueueSummaryAsync(tenantId);

        return Results.Ok(new
        {
            Score = score is null ? null : new
            {
                score.Score,
                Level = score.Level.ToString(),
                score.OpenViolationCount,
                score.CriticalViolationCount,
                score.CalculatedAt
            },
            ViolationTrend = violations,
            ActiveLegalHolds = holds.Count,
            RetentionQueue = retention
        });
    }

    private static async Task<IResult> GetComplianceScore(
        ComplianceDashboardQueryService dashboard,
        ClaimsPrincipal user)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        var score = await dashboard.GetTenantScoreAsync(tenantId);
        return score is null ? Results.NotFound() : Results.Ok(score);
    }

    private static async Task<IResult> GetOpenViolations(
        ComplianceDashboardQueryService dashboard,
        ClaimsPrincipal user,
        int limit = 50)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        var violations = await dashboard.GetOpenViolationsAsync(tenantId, limit);
        return Results.Ok(violations);
    }

    private static async Task<IResult> ResolveViolation(
        Guid id,
        ResolveViolationRequest request,
        Karamchari.Compliance.Persistence.ComplianceDbContext db)
    {
        var violation = await db.PolicyViolations.FindAsync(id);
        if (violation is null) return Results.NotFound();
        violation.Resolve(request.Notes);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> AcceptViolationRisk(
        Guid id,
        ResolveViolationRequest request,
        Karamchari.Compliance.Persistence.ComplianceDbContext db)
    {
        var violation = await db.PolicyViolations.FindAsync(id);
        if (violation is null) return Results.NotFound();
        violation.AcceptRisk(request.Notes);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> StartViolationInvestigation(
        Guid id,
        Karamchari.Compliance.Persistence.ComplianceDbContext db)
    {
        var violation = await db.PolicyViolations.FindAsync(id);
        if (violation is null) return Results.NotFound();
        violation.StartInvestigation();
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetActiveLegalHolds(
        ComplianceDashboardQueryService dashboard,
        ClaimsPrincipal user)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        var holds = await dashboard.GetActiveLegalHoldsAsync(tenantId);
        return Results.Ok(holds);
    }

    private static async Task<IResult> CreateLegalHold(
        CreateLegalHoldRequest request,
        Karamchari.Compliance.Persistence.ComplianceDbContext db,
        ClaimsPrincipal user)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        var createdBy = user.Identity?.Name ?? "HR_Admin";

        var hold = LegalHold.Create(tenantId, request.CaseReference, request.Reason, request.StartDate, createdBy);
        foreach (var scope in request.Scopes)
        {
            hold.AddScope(scope.ScopeType, scope.ScopeValue);
        }
        db.LegalHolds.Add(hold);
        await db.SaveChangesAsync();
        return Results.Created($"/api/governance/legal-holds/{hold.Id}", new { hold.Id });
    }

    private static async Task<IResult> ReleaseLegalHold(
        Guid id,
        Karamchari.Compliance.Persistence.ComplianceDbContext db)
    {
        var hold = await db.LegalHolds.FindAsync(id);
        if (hold is null) return Results.NotFound();
        hold.Release(DateTime.UtcNow);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> RequestAuditPackage(
        AuditPackageRequest request,
        AuditPackageService auditService,
        ClaimsPrincipal user)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        var requestedBy = user.Identity?.Name ?? "Auditor";
        var package = await auditService.RequestPackageAsync(
            tenantId, requestedBy, request.EmployeeId, request.PeriodStart, request.PeriodEnd);
        return Results.Created($"/api/governance/audit-packages/{package.Id}", new { package.Id });
    }

    private static async Task<IResult> GenerateAuditPackage(
        Guid id,
        AuditPackageService auditService)
    {
        await auditService.GeneratePackageAsync(id);
        return Results.NoContent();
    }

    private static async Task<IResult> RecomputeScore(
        ComplianceScoringService scoringService,
        ClaimsPrincipal user)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        await scoringService.RecomputeTenantScoreAsync(tenantId);
        return Results.NoContent();
    }

    private static async Task<IResult> GetScoreTrend(
        ComplianceTrendService trendService,
        ClaimsPrincipal user,
        int days = 30)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        var trend = await trendService.GetScoreTrendAsync(tenantId, days);
        return Results.Ok(trend);
    }

    private static async Task<IResult> GetViolationVelocity(
        ComplianceTrendService trendService,
        ClaimsPrincipal user,
        int days = 30)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        var velocity = await trendService.GetViolationVelocityAsync(tenantId, days);
        return Results.Ok(velocity);
    }

    private static async Task<IResult> GetRiskHeatmap(
        ComplianceTrendService trendService,
        ClaimsPrincipal user)
    {
        var tenantId = user.FindFirst("tenant_id")?.Value ?? string.Empty;
        var heatmap = await trendService.GetRiskHeatmapAsync(tenantId);
        return Results.Ok(heatmap);
    }
}

public sealed record ResolveViolationRequest(string Notes);
public sealed record CreateLegalHoldRequest(
    string CaseReference,
    string Reason,
    DateTime StartDate,
    IReadOnlyList<LegalHoldScopeRequest> Scopes);
public sealed record LegalHoldScopeRequest(LegalHoldScopeType ScopeType, string ScopeValue);
public sealed record AuditPackageRequest(Guid? EmployeeId, DateTime PeriodStart, DateTime PeriodEnd);
