using System.Security.Claims;
using Karamchari.Api.BFF.Common;
using Karamchari.Capability.Domain.Learning;
using Karamchari.Capability.Domain.Primitives;
using Karamchari.Capability.Domain.Skills;
using Karamchari.Capability.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Capability;

public static class CapabilityEndpoints
{
    public static WebApplication MapCapabilityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/capability").RequireAuthorization();

        // Skills
        group.MapGet("/skills", ListSkills).WithName("Capability.Skills.List");
        group.MapPost("/skills", DefineSkill).WithName("Capability.Skills.Define");

        // Profiles
        group.MapGet("/profiles/me", GetMyProfile).WithName("Capability.Profiles.GetMy");
        group.MapPost("/profiles/me/skills", AddVerifiedSkill).WithName("Capability.Profiles.AddSkill");

        // Learning
        group.MapGet("/learning/modules", ListModules).WithName("Capability.Learning.Modules.List");
        group.MapPost("/learning/enrollments/{id:guid}/complete", CompleteEnrollment).WithName("Capability.Learning.Enrollments.Complete");

        return app;
    }

    private static async Task<IResult> ListSkills(CapabilityDbContext db, CancellationToken ct)
    {
        var skills = await db.SkillDefinitions.AsNoTracking().ToListAsync(ct);
        return Results.Ok(skills);
    }

    private static async Task<IResult> DefineSkill(
        [FromBody] DefineSkillRequest request,
        ClaimsPrincipal user,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var skill = SkillDefinition.Define(tenantId, request.Name, request.Category, request.Description);
        
        db.SkillDefinitions.Add(skill);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/capability/skills/{skill.Id}", skill);
    }

    private static async Task<IResult> GetMyProfile(
        ClaimsPrincipal user,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var profile = await db.CapabilityProfiles
            .Include(p => p.Skills)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.EmployeeId == employeeId, ct);

        if (profile == null)
        {
            profile = CapabilityProfile.Initialize(tenantId, employeeId.Value);
            db.CapabilityProfiles.Add(profile);
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(profile);
    }

    private static async Task<IResult> AddVerifiedSkill(
        [FromBody] AddVerifiedSkillRequest request,
        ClaimsPrincipal user,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var profile = await db.CapabilityProfiles
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.EmployeeId == employeeId, ct);

        if (profile == null) return Results.NotFound("Profile not found.");

        if (!Enum.TryParse<SkillLevel>(request.Level, out var level))
            return Results.BadRequest($"Invalid skill level: {request.Level}");

        var verifiedBy = user.Identity?.Name ?? employeeId.ToString()!;

        try
        {
            profile.AddVerifiedSkill(request.SkillId, level, request.EvidenceReference, verifiedBy);
            await db.SaveChangesAsync(ct);
            return Results.Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ListModules(CapabilityDbContext db, CancellationToken ct)
    {
        var modules = await db.LearningModules.AsNoTracking().ToListAsync(ct);
        return Results.Ok(modules);
    }

    private static async Task<IResult> CompleteEnrollment(
        Guid id,
        [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey,
        ClaimsPrincipal user,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Results.BadRequest("X-Idempotency-Key header is required.");

        var enrollment = await db.LearningEnrollments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (enrollment == null) return Results.NotFound();

        enrollment.MarkCompleted(idempotencyKey);
        await db.SaveChangesAsync(ct);

        return Results.Ok(enrollment);
    }
}

public record DefineSkillRequest(string Name, string Category, string Description);
public record AddVerifiedSkillRequest(Guid SkillId, string Level, string EvidenceReference);
