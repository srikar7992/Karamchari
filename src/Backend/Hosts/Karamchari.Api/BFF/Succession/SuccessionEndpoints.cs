using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Succession.Domain;
using Karamchari.Succession.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Succession;

public static class SuccessionEndpoints
{
    public static WebApplication MapSuccessionEndpoints(this WebApplication app)
    {
        var roles = app.MapGroup("/api/v1/succession/critical-roles").RequireAuthorization();
        roles.MapPost("/", RegisterCriticalRole).WithName("Succession.CriticalRoles.Register");
        roles.MapGet("/", ListCriticalRoles).WithName("Succession.CriticalRoles.List");
        roles.MapGet("/{roleId:guid}", GetCriticalRole).WithName("Succession.CriticalRoles.Get");
        roles.MapPut("/{roleId:guid}/incumbent", UpdateIncumbent).WithName("Succession.CriticalRoles.UpdateIncumbent");
        roles.MapDelete("/{roleId:guid}", DeactivateCriticalRole).WithName("Succession.CriticalRoles.Deactivate");

        var plans = app.MapGroup("/api/v1/succession/plans").RequireAuthorization();
        plans.MapPost("/", CreatePlan).WithName("Succession.Plans.Create");
        plans.MapGet("/", ListPlans).WithName("Succession.Plans.List");
        plans.MapGet("/{planId:guid}", GetPlan).WithName("Succession.Plans.Get");
        plans.MapPost("/{planId:guid}/candidates", AddCandidate).WithName("Succession.Plans.AddCandidate");
        plans.MapPut("/{planId:guid}/candidates/{candidateId:guid}/readiness", UpdateReadiness).WithName("Succession.Plans.UpdateReadiness");
        plans.MapPost("/{planId:guid}/candidates/{candidateId:guid}/gaps", AddGap).WithName("Succession.Plans.AddGap");
        plans.MapPost("/{planId:guid}/candidates/{candidateId:guid}/withdraw", WithdrawCandidate).WithName("Succession.Plans.Withdraw");
        plans.MapPost("/{planId:guid}/candidates/{candidateId:guid}/promote", MarkPromoted).WithName("Succession.Plans.MarkPromoted");
        plans.MapPost("/{planId:guid}/review", MarkReviewed).WithName("Succession.Plans.MarkReviewed");

        var analytics = app.MapGroup("/api/v1/succession/analytics").RequireAuthorization();
        analytics.MapGet("/bench-strength", GetBenchStrength).WithName("Succession.Analytics.BenchStrength");
        analytics.MapGet("/nine-box", GetNineBox).WithName("Succession.Analytics.NineBox");

        return app;
    }

    private static async Task<IResult> RegisterCriticalRole(
        [FromBody] RegisterCriticalRoleRequest req,
        ClaimsPrincipal user,
        SuccessionDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var role = CriticalRole.Register(tenantId, req.RoleTitle, req.Department,
            req.CurrentIncumbentEmployeeId?.ToString(), req.Rationale);
        db.CriticalRoles.Add(role);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/succession/critical-roles/{role.Id.Value}", new { role.Id, role.Risk });
    }

    private static async Task<IResult> ListCriticalRoles(ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var roles = await db.CriticalRoles
            .Where(r => r.TenantId == tenantId && r.IsActive)
            .Select(r => new { r.Id, r.RoleTitle, r.Department, r.Risk, r.CurrentIncumbentEmployeeId })
            .OrderBy(r => r.Risk)
            .ToListAsync(ct);
        return Results.Ok(roles);
    }

    private static async Task<IResult> GetCriticalRole(Guid roleId, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var role = await db.CriticalRoles.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == new CriticalRoleId(roleId), ct);
        return role is null ? Results.NotFound() : Results.Ok(role);
    }

    private static async Task<IResult> UpdateIncumbent(Guid roleId, [FromBody] UpdateIncumbentRequest req, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var role = await db.CriticalRoles.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == new CriticalRoleId(roleId), ct);
        if (role is null) return Results.NotFound();

        role.UpdateIncumbent(req.EmployeeId?.ToString());
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeactivateCriticalRole(Guid roleId, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var role = await db.CriticalRoles.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == new CriticalRoleId(roleId), ct);
        if (role is null) return Results.NotFound();

        role.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> CreatePlan([FromBody] CreatePlanRequest req, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var plan = SuccessionPlan.Create(tenantId, new CriticalRoleId(req.CriticalRoleId), employeeId.Value);
        db.SuccessionPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/succession/plans/{plan.Id.Value}", new { plan.Id });
    }

    private static async Task<IResult> ListPlans(ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var plans = await db.SuccessionPlans
            .Where(p => p.TenantId == tenantId)
            .Select(p => new { p.Id, p.RoleId, p.CreatedAt, p.LastReviewedAt, p.BenchStrength })
            .ToListAsync(ct);
        return Results.Ok(plans);
    }

    private static async Task<IResult> GetPlan(Guid planId, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var plan = await db.SuccessionPlans.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == new SuccessionPlanId(planId), ct);
        if (plan is null) return Results.NotFound();

        return Results.Ok(new
        {
            plan.Id,
            plan.RoleId,
            plan.BenchStrength,
            Risk = plan.ComputeRisk(),
            plan.LastReviewedAt,
            Candidates = plan.Candidates.Select(c => new
            {
                c.Id,
                c.EmployeeId,
                c.Readiness,
                c.PerformanceScore,
                c.PotentialScore,
                c.Status,
                c.DevelopmentGaps,
                c.DevelopmentNotes,
                NineBox = c.NineBoxPosition(),
            }),
        });
    }

    private static async Task<IResult> AddCandidate(Guid planId, [FromBody] AddCandidateRequest req, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var plan = await db.SuccessionPlans.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == new SuccessionPlanId(planId), ct);
        if (plan is null) return Results.NotFound();

        var candidate = plan.AddCandidate(req.EmployeeId, req.Readiness, req.PerformanceScore, req.PotentialScore, req.Notes);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/succession/plans/{planId}/candidates/{candidate.Id.Value}", new { candidate.Id, candidate.Readiness, NineBox = candidate.NineBoxPosition() });
    }

    private static async Task<IResult> UpdateReadiness(Guid planId, Guid candidateId, [FromBody] UpdateReadinessRequest req, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var plan = await db.SuccessionPlans.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == new SuccessionPlanId(planId), ct);
        if (plan is null) return Results.NotFound();

        plan.UpdateCandidateReadiness(new SuccessorCandidateId(candidateId), req.Readiness, req.Notes);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> AddGap(Guid planId, Guid candidateId, [FromBody] AddGapRequest req, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var plan = await db.SuccessionPlans.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == new SuccessionPlanId(planId), ct);
        if (plan is null) return Results.NotFound();

        plan.AddDevelopmentGap(new SuccessorCandidateId(candidateId), req.Gap);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> WithdrawCandidate(Guid planId, Guid candidateId, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var plan = await db.SuccessionPlans.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == new SuccessionPlanId(planId), ct);
        if (plan is null) return Results.NotFound();

        plan.WithdrawCandidate(new SuccessorCandidateId(candidateId));
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> MarkPromoted(Guid planId, Guid candidateId, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var plan = await db.SuccessionPlans.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == new SuccessionPlanId(planId), ct);
        if (plan is null) return Results.NotFound();

        plan.MarkCandidatePromoted(new SuccessorCandidateId(candidateId));
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> MarkReviewed(Guid planId, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var plan = await db.SuccessionPlans.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == new SuccessionPlanId(planId), ct);
        if (plan is null) return Results.NotFound();

        plan.MarkReviewed();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { plan.LastReviewedAt });
    }

    private static async Task<IResult> GetBenchStrength(ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var roles = await db.CriticalRoles
            .Where(r => r.TenantId == tenantId && r.IsActive)
            .ToListAsync(ct);

        var plans = await db.SuccessionPlans
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(ct);

        var report = roles.Select(r =>
        {
            var plan = plans.FirstOrDefault(p => p.RoleId == r.Id);
            return new
            {
                r.Id,
                r.RoleTitle,
                r.Department,
                CurrentRisk = r.Risk,
                BenchStrength = plan?.BenchStrength ?? 0,
                ComputedRisk = plan?.ComputeRisk() ?? SuccessionRisk.Critical,
                LastReviewedAt = plan?.LastReviewedAt,
            };
        });

        var summary = new
        {
            TotalCriticalRoles = roles.Count,
            CriticalRisk = roles.Count(r => r.Risk == SuccessionRisk.Critical),
            HighRisk = roles.Count(r => r.Risk == SuccessionRisk.High),
            Roles = report,
        };

        return Results.Ok(summary);
    }

    private static async Task<IResult> GetNineBox(ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var plans = await db.SuccessionPlans
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(ct);

        var grid = plans
            .SelectMany(p => p.Candidates)
            .Where(c => c.Status == SuccessorStatus.Active)
            .Select(c =>
            {
                var (perf, pot) = c.NineBoxPosition();
                return new
                {
                    c.EmployeeId,
                    c.Readiness,
                    PerformanceBucket = perf,
                    PotentialBucket = pot,
                    Box = $"P{perf}x{pot}",
                };
            })
            .GroupBy(x => x.Box)
            .Select(g => new { Box = g.Key, Count = g.Count(), Employees = g.Select(x => x.EmployeeId) });

        return Results.Ok(grid);
    }

    private sealed record RegisterCriticalRoleRequest(string RoleTitle, string? Department, Guid? CurrentIncumbentEmployeeId, string? Rationale);
    private sealed record UpdateIncumbentRequest(Guid? EmployeeId);
    private sealed record CreatePlanRequest(Guid CriticalRoleId);
    private sealed record AddCandidateRequest(Guid EmployeeId, ReadinessLevel Readiness, int PerformanceScore, int PotentialScore, string? Notes);
    private sealed record UpdateReadinessRequest(ReadinessLevel Readiness, string? Notes);
    private sealed record AddGapRequest(string Gap);
}
