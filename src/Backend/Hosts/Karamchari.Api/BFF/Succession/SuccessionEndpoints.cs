using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Capability.Domain.Primitives;
using Karamchari.Capability.Domain.Skills;
using Karamchari.Capability.Persistence;
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

        var paths = app.MapGroup("/api/v1/succession/career-paths").RequireAuthorization();
        paths.MapPost("/", CreateCareerPath).WithName("Succession.CareerPaths.Create");
        paths.MapGet("/", ListCareerPaths).WithName("Succession.CareerPaths.List");
        paths.MapGet("/{pathId:guid}", GetCareerPath).WithName("Succession.CareerPaths.Get");
        paths.MapPut("/{pathId:guid}", UpdateCareerPath).WithName("Succession.CareerPaths.Update");
        paths.MapDelete("/{pathId:guid}", DeactivateCareerPath).WithName("Succession.CareerPaths.Deactivate");
        paths.MapPost("/{pathId:guid}/steps", AddCareerStep).WithName("Succession.CareerPaths.AddStep");
        paths.MapDelete("/{pathId:guid}/steps/{stepId:guid}", RemoveCareerStep).WithName("Succession.CareerPaths.RemoveStep");
        paths.MapGet("/{pathId:guid}/progression/{employeeId:guid}", GetCareerProgression).WithName("Succession.CareerPaths.Progression");

        return app;
    }

    // ── Critical Roles ────────────────────────────────────────────────────────

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

    // ── Succession Plans ──────────────────────────────────────────────────────

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

    // ── Analytics ─────────────────────────────────────────────────────────────

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

    // ── Career Paths ──────────────────────────────────────────────────────────

    private static async Task<IResult> CreateCareerPath(
        [FromBody] CreateCareerPathRequest req,
        ClaimsPrincipal user,
        SuccessionDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var path = CareerPath.Create(tenantId, req.Name, req.Description ?? string.Empty);
        db.CareerPaths.Add(path);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/succession/career-paths/{path.Id}", new { path.Id, path.Name });
    }

    private static async Task<IResult> ListCareerPaths(ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var paths = await db.CareerPaths
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .Select(p => new { p.Id, p.Name, p.Description, StepCount = p.Steps.Count })
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
        return Results.Ok(paths);
    }

    private static async Task<IResult> GetCareerPath(Guid pathId, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var path = await db.CareerPaths.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == pathId, ct);
        if (path is null) return Results.NotFound();

        return Results.Ok(new
        {
            path.Id,
            path.Name,
            path.Description,
            path.IsActive,
            Steps = path.Steps.OrderBy(s => s.StepOrder).Select(s => new
            {
                s.Id,
                s.StepOrder,
                s.RoleTitle,
                s.RoleSkillRequirementId,
                s.Notes,
            }),
        });
    }

    private static async Task<IResult> UpdateCareerPath(Guid pathId, [FromBody] UpdateCareerPathRequest req, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var path = await db.CareerPaths.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == pathId, ct);
        if (path is null) return Results.NotFound();

        path.Update(req.Name, req.Description ?? string.Empty);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeactivateCareerPath(Guid pathId, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var path = await db.CareerPaths.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == pathId, ct);
        if (path is null) return Results.NotFound();

        path.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> AddCareerStep(Guid pathId, [FromBody] AddCareerStepRequest req, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var path = await db.CareerPaths.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == pathId, ct);
        if (path is null) return Results.NotFound();

        var step = path.AddStep(req.StepOrder, req.RoleTitle, req.RoleSkillRequirementId, req.Notes);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/succession/career-paths/{pathId}/steps/{step.Id}", new { step.Id, step.StepOrder, step.RoleTitle });
    }

    private static async Task<IResult> RemoveCareerStep(Guid pathId, Guid stepId, ClaimsPrincipal user, SuccessionDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var path = await db.CareerPaths.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == pathId, ct);
        if (path is null) return Results.NotFound();

        path.RemoveStep(stepId);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    /// <summary>
    /// Returns an employee's current skill level vs each career step's required skills.
    /// Shows which steps they meet, partially meet, or are blocked by skill gaps.
    /// </summary>
    private static async Task<IResult> GetCareerProgression(
        Guid pathId,
        Guid employeeId,
        ClaimsPrincipal user,
        SuccessionDbContext successionDb,
        CapabilityDbContext capabilityDb,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var path = await successionDb.CareerPaths.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == pathId, ct);
        if (path is null) return Results.NotFound();

        var profile = await capabilityDb.CapabilityProfiles
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.EmployeeId == employeeId, ct);

        var skillMap = profile?.Skills.ToDictionary(s => s.SkillId, s => s.Level)
            ?? new Dictionary<Guid, SkillLevel>();

        var requirementIds = path.Steps
            .Where(s => s.RoleSkillRequirementId.HasValue)
            .Select(s => s.RoleSkillRequirementId!.Value)
            .Distinct()
            .ToList();

        var requirements = await capabilityDb.RoleSkillRequirements
            .Where(r => requirementIds.Contains(r.Id))
            .ToListAsync(ct);

        var stepResults = path.Steps.OrderBy(s => s.StepOrder).Select(step =>
        {
            if (!step.RoleSkillRequirementId.HasValue)
                return new StepProgressionResult(step.Id, step.StepOrder, step.RoleTitle, true, 100, []);

            var req = requirements.FirstOrDefault(r => r.Id == step.RoleSkillRequirementId.Value);
            if (req is null)
                return new StepProgressionResult(step.Id, step.StepOrder, step.RoleTitle, true, 100, []);

            var gaps = req.Skills
                .Select(rs =>
                {
                    skillMap.TryGetValue(rs.SkillId, out var current);
                    bool met = current >= rs.MinimumLevel;
                    return new StepSkillGap(rs.SkillId, rs.MinimumLevel, current, met);
                })
                .ToList();

            int metCount = gaps.Count(g => g.Met);
            int pct = req.Skills.Count == 0 ? 100 : (int)Math.Round(100.0 * metCount / req.Skills.Count);
            bool ready = gaps.All(g => g.Met);

            return new StepProgressionResult(step.Id, step.StepOrder, step.RoleTitle, ready, pct, gaps);
        }).ToList();

        return Results.Ok(new
        {
            PathId = pathId,
            PathName = path.Name,
            EmployeeId = employeeId,
            Steps = stepResults,
            CurrentStep = stepResults.LastOrDefault(s => s.Ready)?.StepOrder ?? 0,
            NextStep = stepResults.FirstOrDefault(s => !s.Ready)?.StepOrder,
        });
    }

    // ── Request / Response records ─────────────────────────────────────────────

    private sealed record RegisterCriticalRoleRequest(string RoleTitle, string? Department, Guid? CurrentIncumbentEmployeeId, string? Rationale);
    private sealed record UpdateIncumbentRequest(Guid? EmployeeId);
    private sealed record CreatePlanRequest(Guid CriticalRoleId);
    private sealed record AddCandidateRequest(Guid EmployeeId, ReadinessLevel Readiness, int PerformanceScore, int PotentialScore, string? Notes);
    private sealed record UpdateReadinessRequest(ReadinessLevel Readiness, string? Notes);
    private sealed record AddGapRequest(string Gap);
    private sealed record CreateCareerPathRequest(string Name, string? Description);
    private sealed record UpdateCareerPathRequest(string Name, string? Description);
    private sealed record AddCareerStepRequest(int StepOrder, string RoleTitle, Guid? RoleSkillRequirementId, string? Notes);
    private sealed record StepProgressionResult(Guid StepId, int StepOrder, string RoleTitle, bool Ready, int ReadinessPercent, List<StepSkillGap> Gaps);
    private sealed record StepSkillGap(Guid SkillId, SkillLevel Required, SkillLevel? Current, bool Met);
}
