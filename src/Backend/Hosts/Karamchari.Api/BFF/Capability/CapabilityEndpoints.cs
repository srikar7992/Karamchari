using System.Security.Claims;
using Karamchari.Api.BFF.Common;
using Karamchari.Capability.Domain.Learning;
using Karamchari.Capability.Domain.Primitives;
using Karamchari.Capability.Domain.Skills;
using Karamchari.Capability.Persistence;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Karamchari.Core.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Capability;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class CapabilityEndpoints
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapCapabilityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/capability").RequireAuthorization();

        // ── Skill Categories ──────────────────────────────────────────────
        group.MapGet("/skill-categories", ListSkillCategories)
            .RequireAuthorization(Permissions.CapabilityRead)
            .WithName("Capability.SkillCategories.List");

        group.MapPost("/skill-categories", CreateSkillCategory)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.SkillCategories.Create");

        group.MapDelete("/skill-categories/{categoryId:guid}", DeactivateSkillCategory)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.SkillCategories.Deactivate");

        // ── Skills ────────────────────────────────────────────────────────
        group.MapGet("/skills", ListSkills)
            .RequireAuthorization(Permissions.CapabilityRead)
            .WithName("Capability.Skills.List");

        group.MapPost("/skills", DefineSkill)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.Skills.Define");

        group.MapPost("/skills/{skillId:guid}/deprecate", DeprecateSkill)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.Skills.Deprecate");

        // ── Certification Catalog ─────────────────────────────────────────
        group.MapGet("/certification-definitions", ListCertificationDefinitions)
            .RequireAuthorization(Permissions.CapabilityRead)
            .WithName("Capability.CertificationDefinitions.List");

        group.MapPost("/certification-definitions", CreateCertificationDefinition)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.CertificationDefinitions.Create");

        group.MapGet("/certification-definitions/{definitionId:guid}", GetCertificationDefinition)
            .RequireAuthorization(Permissions.CapabilityRead)
            .WithName("Capability.CertificationDefinitions.Get");

        group.MapPost("/certification-definitions/{definitionId:guid}/deactivate", DeactivateCertificationDefinition)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.CertificationDefinitions.Deactivate");

        // ── Employee Profiles ─────────────────────────────────────────────
        group.MapGet("/profiles/me", GetMyProfile)
            .RequireAuthorization(Permissions.CapabilityRead)
            .WithName("Capability.Profiles.GetMy");

        group.MapGet("/profiles/{employeeId:guid}", GetEmployeeProfile)
            .RequireAuthorization(Permissions.CapabilityRead)
            .WithName("Capability.Profiles.GetEmployee");

        group.MapPost("/profiles/me/skills", AddVerifiedSkill)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.Profiles.AddSkill");

        group.MapPut("/profiles/me/skills/{skillId:guid}", UpdateVerifiedSkill)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.Profiles.UpdateSkill");

        // ── Employee Certifications ────────────────────────────────────────
        group.MapGet("/profiles/me/certifications", ListMyCertifications)
            .RequireAuthorization(Permissions.CapabilityRead)
            .WithName("Capability.Profiles.Certifications.List");

        group.MapPost("/profiles/me/certifications", RecordCertificationAchievement)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.Profiles.Certifications.Record");

        group.MapPost("/profiles/me/certifications/{certId:guid}/revoke", RevokeCertificationAchievement)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.Profiles.Certifications.Revoke");

        // ── Gap Analysis ──────────────────────────────────────────────────
        group.MapGet("/gap-analysis", ComputeGapAnalysis)
            .RequireAuthorization(Permissions.CapabilityRead)
            .WithName("Capability.GapAnalysis");

        group.MapGet("/gap-analysis/{employeeId:guid}", ComputeEmployeeGapAnalysis)
            .RequireAuthorization(Permissions.CapabilityRead)
            .WithName("Capability.GapAnalysis.Employee");

        // ── Learning ──────────────────────────────────────────────────────
        group.MapGet("/learning/modules", ListModules)
            .RequireAuthorization(Permissions.CapabilityRead)
            .WithName("Capability.Learning.Modules.List");

        group.MapPost("/learning/modules", CreateLearningModule)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.Learning.Modules.Create");

        group.MapGet("/learning/modules/{moduleId:guid}", GetLearningModule)
            .RequireAuthorization(Permissions.CapabilityRead)
            .WithName("Capability.Learning.Modules.Get");

        group.MapPost("/learning/modules/{moduleId:guid}/skills", LinkSkillToModule)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.Learning.Modules.LinkSkill");

        group.MapDelete("/learning/modules/{moduleId:guid}/skills/{skillId:guid}", UnlinkSkillFromModule)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.Learning.Modules.UnlinkSkill");

        group.MapPost("/learning/modules/{moduleId:guid}/deactivate", DeactivateLearningModule)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.Learning.Modules.Deactivate");

        group.MapGet("/learning/enrollments", ListMyEnrollments)
            .RequireAuthorization(Permissions.CapabilityRead)
            .WithName("Capability.Learning.Enrollments.List");

        group.MapPost("/learning/enrollments", EnrollInModule)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.Learning.Enrollments.Enroll");

        group.MapPost("/learning/enrollments/{id:guid}/start", StartEnrollment)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.Learning.Enrollments.Start");

        group.MapPost("/learning/enrollments/{id:guid}/complete", CompleteEnrollment)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.Learning.Enrollments.Complete");

        group.MapGet("/learning/recommendations", GetLearningRecommendations)
            .RequireAuthorization(Permissions.CapabilityRead)
            .WithName("Capability.Learning.Recommendations");

        // ── Role Skill Requirements ────────────────────────────────────────
        group.MapGet("/role-requirements", ListRoleRequirements)
            .RequireAuthorization(Permissions.CapabilityRead)
            .WithName("Capability.RoleRequirements.List");

        group.MapPost("/role-requirements", CreateRoleRequirement)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.RoleRequirements.Create");

        group.MapGet("/role-requirements/{requirementId:guid}", GetRoleRequirement)
            .RequireAuthorization(Permissions.CapabilityRead)
            .WithName("Capability.RoleRequirements.Get");

        group.MapPost("/role-requirements/{requirementId:guid}/skills", AddRequiredSkill)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.RoleRequirements.AddSkill");

        group.MapDelete("/role-requirements/{requirementId:guid}/skills/{skillId:guid}", RemoveRequiredSkill)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.RoleRequirements.RemoveSkill");

        group.MapPost("/role-requirements/{requirementId:guid}/deactivate", DeactivateRoleRequirement)
            .RequireAuthorization(Permissions.CapabilityWrite)
            .WithName("Capability.RoleRequirements.Deactivate");

        return app;
    }

    // ── Skill Categories ──────────────────────────────────────────────────

    private static async Task<IResult> ListSkillCategories(CapabilityDbContext db, CancellationToken ct)
    {
        var categories = await db.SkillCategories.AsNoTracking().ToListAsync(ct);
        return Results.Ok(categories);
    }

    private static async Task<IResult> CreateSkillCategory(
        [FromBody] CreateSkillCategoryRequest request,
        TenantExecutionContextAccessor contextAccessor,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var tenantId = contextAccessor.Current!.TenantId;
        var category = SkillCategory.Create(tenantId, request.Name, request.Description ?? string.Empty, request.ParentCategoryId);
        db.SkillCategories.Add(category);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/capability/skill-categories/{category.Id}", category);
    }

    private static async Task<IResult> DeactivateSkillCategory(
        Guid categoryId,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var category = await db.SkillCategories.FirstOrDefaultAsync(c => c.Id == categoryId, ct);
        if (category is null) return Results.NotFound();
        category.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.Ok(category);
    }

    // ── Skills ──────────────────────────────────────────────────────────

    private static async Task<IResult> ListSkills(
        [FromQuery] Guid? categoryId,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var query = db.SkillDefinitions.AsNoTracking();
        if (categoryId.HasValue)
            query = query.Where(s => s.CategoryId == categoryId.Value);
        var skills = await query.ToListAsync(ct);
        return Results.Ok(skills);
    }

    private static async Task<IResult> DefineSkill(
        [FromBody] DefineSkillRequest request,
        TenantExecutionContextAccessor contextAccessor,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var context = contextAccessor.Current!;
        var skill = SkillDefinition.Define(
            context.TenantId,
            request.Name,
            request.Category,
            request.Description,
            request.CategoryId);

        db.SkillDefinitions.Add(skill);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/capability/skills/{skill.Id}", skill);
    }

    private static async Task<IResult> DeprecateSkill(
        Guid skillId,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var skill = await db.SkillDefinitions.FirstOrDefaultAsync(s => s.Id == skillId, ct);
        if (skill is null) return Results.NotFound();
        skill.Deprecate();
        await db.SaveChangesAsync(ct);
        return Results.Ok(skill);
    }

    // ── Certification Catalog ─────────────────────────────────────────────

    private static async Task<IResult> ListCertificationDefinitions(CapabilityDbContext db, CancellationToken ct)
    {
        var defs = await db.CertificationDefinitions.AsNoTracking().ToListAsync(ct);
        return Results.Ok(defs);
    }

    private static async Task<IResult> CreateCertificationDefinition(
        [FromBody] CreateCertificationDefinitionRequest request,
        TenantExecutionContextAccessor contextAccessor,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var tenantId = contextAccessor.Current!.TenantId;
        var def = CertificationDefinition.Create(
            tenantId,
            request.Name,
            request.Issuer,
            request.Description ?? string.Empty,
            request.DefaultValidityMonths,
            request.RenewalLeadTimeMonths);

        db.CertificationDefinitions.Add(def);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/capability/certification-definitions/{def.Id}", def);
    }

    private static async Task<IResult> GetCertificationDefinition(
        Guid definitionId,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var def = await db.CertificationDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == definitionId, ct);
        return def is null ? Results.NotFound() : Results.Ok(def);
    }

    private static async Task<IResult> DeactivateCertificationDefinition(
        Guid definitionId,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var def = await db.CertificationDefinitions.FirstOrDefaultAsync(d => d.Id == definitionId, ct);
        if (def is null) return Results.NotFound();
        def.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.Ok(def);
    }

    // ── Employee Profiles ─────────────────────────────────────────────────

    private static async Task<IResult> GetMyProfile(
        ClaimsPrincipal user,
        TenantExecutionContextAccessor contextAccessor,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var context = contextAccessor.Current!;
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var profile = await db.CapabilityProfiles
            .Include(p => p.Skills)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId, ct);

        if (profile is null)
        {
            profile = CapabilityProfile.Initialize(context.TenantId, employeeId.Value);
            db.CapabilityProfiles.Add(profile);
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(profile);
    }

    private static async Task<IResult> GetEmployeeProfile(
        Guid employeeId,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var profile = await db.CapabilityProfiles
            .Include(p => p.Skills)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId, ct);
        return profile is null ? Results.NotFound() : Results.Ok(profile);
    }

    private static async Task<IResult> AddVerifiedSkill(
        [FromBody] AddVerifiedSkillRequest request,
        ClaimsPrincipal user,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var profile = await db.CapabilityProfiles
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId, ct);

        if (profile is null) return Results.NotFound("Profile not found.");

        if (!Enum.TryParse<SkillLevel>(request.Level, out var level))
            return Results.BadRequest($"Invalid skill level: {request.Level}");

        var verifiedBy = user.Identity?.Name ?? employeeId.ToString()!;

        try
        {
            profile.AddVerifiedSkill(request.SkillId, level, request.EvidenceReference, verifiedBy, request.ValidUntil);
            await db.SaveChangesAsync(ct);
            return Results.Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpdateVerifiedSkill(
        Guid skillId,
        [FromBody] UpdateVerifiedSkillRequest request,
        ClaimsPrincipal user,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var profile = await db.CapabilityProfiles
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId, ct);

        if (profile is null) return Results.NotFound("Profile not found.");

        if (!Enum.TryParse<SkillLevel>(request.Level, out var level))
            return Results.BadRequest($"Invalid skill level: {request.Level}");

        var verifiedBy = user.Identity?.Name ?? employeeId.ToString()!;

        try
        {
            profile.UpdateSkillLevel(skillId, level, request.EvidenceReference, verifiedBy, request.ValidUntil);
            await db.SaveChangesAsync(ct);
            return Results.Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    // ── Employee Certifications ────────────────────────────────────────────

    private static async Task<IResult> ListMyCertifications(
        ClaimsPrincipal user,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var certs = await db.CertificationAchievements
            .AsNoTracking()
            .Where(c => c.EmployeeId == employeeId)
            .ToListAsync(ct);

        return Results.Ok(certs);
    }

    private static async Task<IResult> RecordCertificationAchievement(
        [FromBody] RecordCertificationRequest request,
        ClaimsPrincipal user,
        TenantExecutionContextAccessor contextAccessor,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var tenantId = contextAccessor.Current!.TenantId;

        DateTimeOffset? expiresAt = null;
        if (request.ExpiresAt.HasValue)
        {
            expiresAt = request.ExpiresAt.Value;
        }
        else if (request.CertificationDefinitionId.HasValue)
        {
            var def = await db.CertificationDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == request.CertificationDefinitionId.Value, ct);
            if (def?.DefaultValidityMonths.HasValue == true)
                expiresAt = request.IssuedAt.AddMonths(def.DefaultValidityMonths.Value);
        }

        var cert = CertificationAchievement.Record(
            tenantId,
            employeeId.Value,
            request.Name,
            request.Authority,
            request.LicenseNumber ?? string.Empty,
            request.IssuedAt,
            expiresAt,
            request.CertificationDefinitionId);

        db.CertificationAchievements.Add(cert);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/capability/profiles/me/certifications/{cert.Id}", cert);
    }

    private static async Task<IResult> RevokeCertificationAchievement(
        Guid certId,
        [FromBody] RevokeRequest request,
        ClaimsPrincipal user,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var cert = await db.CertificationAchievements
            .FirstOrDefaultAsync(c => c.Id == certId && c.EmployeeId == employeeId, ct);

        if (cert is null) return Results.NotFound();

        cert.Revoke(request.Reason);
        await db.SaveChangesAsync(ct);

        return Results.Ok(cert);
    }

    // ── Gap Analysis ──────────────────────────────────────────────────────

    private static async Task<IResult> ComputeGapAnalysis(
        [FromQuery] Guid roleRequirementId,
        ClaimsPrincipal user,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        return await RunGapAnalysis(roleRequirementId, employeeId.Value, db, ct);
    }

    private static async Task<IResult> ComputeEmployeeGapAnalysis(
        Guid employeeId,
        [FromQuery] Guid roleRequirementId,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        return await RunGapAnalysis(roleRequirementId, employeeId, db, ct);
    }

    private static async Task<IResult> RunGapAnalysis(
        Guid roleRequirementId,
        Guid employeeId,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var requirement = await db.RoleSkillRequirements
            .AsNoTracking()
            .Include(r => r.Skills)
            .FirstOrDefaultAsync(r => r.Id == roleRequirementId, ct);

        if (requirement is null) return Results.NotFound("Role requirement not found.");

        var profile = await db.CapabilityProfiles
            .AsNoTracking()
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId, ct);

        if (profile is null)
            return Results.Ok(new GapAnalysisResult(roleRequirementId, employeeId, requirement.RoleTitle, [], requirement.Skills.Count));

        var gaps = requirement.ComputeGaps(profile);

        var skillIds = gaps.Select(g => g.SkillId)
            .Union(profile.Skills.Select(s => s.SkillId))
            .Distinct()
            .ToList();

        var skillNames = await db.SkillDefinitions
            .AsNoTracking()
            .Where(s => skillIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name, s.Category })
            .ToDictionaryAsync(s => s.Id, ct);

        var gapDetails = gaps.Select(g => new GapDetail(
            g.SkillId,
            skillNames.GetValueOrDefault(g.SkillId)?.Name ?? g.SkillId.ToString(),
            skillNames.GetValueOrDefault(g.SkillId)?.Category ?? string.Empty,
            g.Required.ToString(),
            g.Actual?.ToString(),
            g.IsMissing,
            g.LevelsBehind)).ToList();

        var profileSkillCount = profile.Skills.Count;
        var readyCount = requirement.Skills.Count - gaps.Count;
        var readinessPercent = requirement.Skills.Count == 0
            ? 100m
            : Math.Round((decimal)readyCount / requirement.Skills.Count * 100, 1);

        return Results.Ok(new GapAnalysisResult(
            roleRequirementId,
            employeeId,
            requirement.RoleTitle,
            gapDetails,
            requirement.Skills.Count,
            profileSkillCount,
            readinessPercent));
    }

    // ── Role Skill Requirements ────────────────────────────────────────────

    private static async Task<IResult> ListRoleRequirements(
        [FromQuery] string? jobFamily,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var query = db.RoleSkillRequirements.AsNoTracking().Where(r => r.IsActive);
        if (!string.IsNullOrWhiteSpace(jobFamily))
            query = query.Where(r => r.JobFamily == jobFamily);
        var results = await query.ToListAsync(ct);
        return Results.Ok(results);
    }

    private static async Task<IResult> CreateRoleRequirement(
        [FromBody] CreateRoleRequirementRequest request,
        TenantExecutionContextAccessor contextAccessor,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var tenantId = contextAccessor.Current!.TenantId;
        var req = RoleSkillRequirement.Define(tenantId, request.RoleTitle, request.JobFamily, request.Department);
        db.RoleSkillRequirements.Add(req);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/capability/role-requirements/{req.Id}", req);
    }

    private static async Task<IResult> GetRoleRequirement(
        Guid requirementId,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var req = await db.RoleSkillRequirements
            .AsNoTracking()
            .Include(r => r.Skills)
            .FirstOrDefaultAsync(r => r.Id == requirementId, ct);
        return req is null ? Results.NotFound() : Results.Ok(req);
    }

    private static async Task<IResult> AddRequiredSkill(
        Guid requirementId,
        [FromBody] AddRequiredSkillRequest request,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var req = await db.RoleSkillRequirements
            .Include(r => r.Skills)
            .FirstOrDefaultAsync(r => r.Id == requirementId, ct);

        if (req is null) return Results.NotFound();

        if (!Enum.TryParse<SkillLevel>(request.MinimumLevel, out var level))
            return Results.BadRequest($"Invalid skill level: {request.MinimumLevel}");

        try
        {
            req.AddRequiredSkill(request.SkillId, level, request.IsMandatory ?? true);
            await db.SaveChangesAsync(ct);
            return Results.Ok(req);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> RemoveRequiredSkill(
        Guid requirementId,
        Guid skillId,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var req = await db.RoleSkillRequirements
            .Include(r => r.Skills)
            .FirstOrDefaultAsync(r => r.Id == requirementId, ct);

        if (req is null) return Results.NotFound();
        req.RemoveSkill(skillId);
        await db.SaveChangesAsync(ct);
        return Results.Ok(req);
    }

    private static async Task<IResult> DeactivateRoleRequirement(
        Guid requirementId,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var req = await db.RoleSkillRequirements.FirstOrDefaultAsync(r => r.Id == requirementId, ct);
        if (req is null) return Results.NotFound();
        req.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.Ok(req);
    }

    // ── Learning ──────────────────────────────────────────────────────────

    private static async Task<IResult> ListModules(
        [FromQuery] bool? activeOnly,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var query = db.LearningModules.AsNoTracking();
        if (activeOnly is true)
            query = query.Where(m => m.IsActive);
        var modules = await query.ToListAsync(ct);
        return Results.Ok(modules);
    }

    private static async Task<IResult> CreateLearningModule(
        [FromBody] CreateLearningModuleRequest request,
        TenantExecutionContextAccessor contextAccessor,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var tenantId = contextAccessor.Current!.TenantId;
        var module = LearningModule.Create(
            tenantId,
            request.Title,
            request.Description ?? string.Empty,
            request.EstimatedMinutes,
            request.ExternalProviderId,
            request.ContentUrl);
        db.LearningModules.Add(module);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/capability/learning/modules/{module.Id}", module);
    }

    private static async Task<IResult> GetLearningModule(
        Guid moduleId,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var module = await db.LearningModules
            .AsNoTracking()
            .Include(m => m.SkillTargets)
            .FirstOrDefaultAsync(m => m.Id == moduleId, ct);
        return module is null ? Results.NotFound() : Results.Ok(module);
    }

    private static async Task<IResult> LinkSkillToModule(
        Guid moduleId,
        [FromBody] LinkSkillToModuleRequest request,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var module = await db.LearningModules
            .Include(m => m.SkillTargets)
            .FirstOrDefaultAsync(m => m.Id == moduleId, ct);

        if (module is null) return Results.NotFound();

        if (!Enum.TryParse<SkillLevel>(request.GrantedLevel, out var level))
            return Results.BadRequest($"Invalid skill level: {request.GrantedLevel}");

        try
        {
            module.LinkSkill(request.SkillId, level);
            await db.SaveChangesAsync(ct);
            return Results.Ok(module);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UnlinkSkillFromModule(
        Guid moduleId,
        Guid skillId,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var module = await db.LearningModules
            .Include(m => m.SkillTargets)
            .FirstOrDefaultAsync(m => m.Id == moduleId, ct);

        if (module is null) return Results.NotFound();
        module.RemoveSkillLink(skillId);
        await db.SaveChangesAsync(ct);
        return Results.Ok(module);
    }

    private static async Task<IResult> DeactivateLearningModule(
        Guid moduleId,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var module = await db.LearningModules.FirstOrDefaultAsync(m => m.Id == moduleId, ct);
        if (module is null) return Results.NotFound();
        module.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.Ok(module);
    }

    private static async Task<IResult> ListMyEnrollments(
        ClaimsPrincipal user,
        [FromQuery] string? status,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var query = db.LearningEnrollments.AsNoTracking().Where(e => e.EmployeeId == employeeId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EnrollmentStatus>(status, out var statusEnum))
            query = query.Where(e => e.Status == statusEnum);

        var enrollments = await query.ToListAsync(ct);
        return Results.Ok(enrollments);
    }

    private static async Task<IResult> EnrollInModule(
        [FromBody] EnrollRequest request,
        ClaimsPrincipal user,
        TenantExecutionContextAccessor contextAccessor,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var tenantId = contextAccessor.Current!.TenantId;

        var alreadyEnrolled = await db.LearningEnrollments.AnyAsync(
            e => e.TenantId == tenantId
              && e.EmployeeId == employeeId
              && e.ModuleId == request.ModuleId
              && e.Status != EnrollmentStatus.Dropped
              && e.Status != EnrollmentStatus.Failed,
            ct);

        if (alreadyEnrolled)
            return Results.Conflict(new { error = "Already enrolled in this module." });

        var enrollment = LearningEnrollment.Assign(
            tenantId,
            employeeId.Value,
            request.ModuleId,
            request.IsMandatory ?? false,
            request.Deadline,
            request.TargetSkillId);

        db.LearningEnrollments.Add(enrollment);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/capability/learning/enrollments/{enrollment.Id}", enrollment);
    }

    private static async Task<IResult> StartEnrollment(
        Guid id,
        ClaimsPrincipal user,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var enrollment = await db.LearningEnrollments
            .FirstOrDefaultAsync(e => e.Id == id && e.EmployeeId == employeeId, ct);

        if (enrollment is null) return Results.NotFound();

        try
        {
            enrollment.Start();
            await db.SaveChangesAsync(ct);
            return Results.Ok(enrollment);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> CompleteEnrollment(
        Guid id,
        [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Results.BadRequest("X-Idempotency-Key header is required.");

        var enrollment = await db.LearningEnrollments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (enrollment is null) return Results.NotFound();

        enrollment.MarkCompleted(idempotencyKey);
        await db.SaveChangesAsync(ct);

        return Results.Ok(enrollment);
    }

    private static async Task<IResult> GetLearningRecommendations(
        [FromQuery] Guid roleRequirementId,
        ClaimsPrincipal user,
        CapabilityDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var requirement = await db.RoleSkillRequirements
            .AsNoTracking()
            .Include(r => r.Skills)
            .FirstOrDefaultAsync(r => r.Id == roleRequirementId, ct);

        if (requirement is null) return Results.NotFound("Role requirement not found.");

        var profile = await db.CapabilityProfiles
            .AsNoTracking()
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId, ct);

        var gaps = profile is null
            ? requirement.Skills.Select(s => new SkillGap(s.SkillId, s.MinimumLevel, null)).ToList()
            : requirement.ComputeGaps(profile);

        if (gaps.Count == 0)
            return Results.Ok(new LearningRecommendationResult(roleRequirementId, requirement.RoleTitle, []));

        var gapSkillIds = gaps.Select(g => g.SkillId).ToList();

        var modules = await db.LearningModules
            .AsNoTracking()
            .Include(m => m.SkillTargets)
            .Where(m => m.IsActive && m.SkillTargets.Any(t => gapSkillIds.Contains(t.SkillId)))
            .ToListAsync(ct);

        var skillNames = await db.SkillDefinitions
            .AsNoTracking()
            .Where(s => gapSkillIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name })
            .ToDictionaryAsync(s => s.Id, ct);

        var recommendations = gaps.Select(gap =>
        {
            var matchingModules = modules
                .Where(m => m.SkillTargets.Any(t =>
                    t.SkillId == gap.SkillId && t.GrantedLevel >= gap.Required))
                .Select(m => new ModuleRecommendation(m.Id, m.Title, m.EstimatedMinutes, m.ContentUrl))
                .ToList();

            return new SkillRecommendation(
                gap.SkillId,
                skillNames.GetValueOrDefault(gap.SkillId)?.Name ?? gap.SkillId.ToString(),
                gap.Required.ToString(),
                gap.Actual?.ToString(),
                gap.IsMissing,
                matchingModules);
        }).ToList();

        return Results.Ok(new LearningRecommendationResult(roleRequirementId, requirement.RoleTitle, recommendations));
    }
}

// ── Request / Response records ────────────────────────────────────────────────

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record DefineSkillRequest(string Name, string Category, string Description, Guid? CategoryId = null);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record AddVerifiedSkillRequest(Guid SkillId, string Level, string EvidenceReference, DateTimeOffset? ValidUntil = null);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record UpdateVerifiedSkillRequest(string Level, string EvidenceReference, DateTimeOffset? ValidUntil = null);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CreateSkillCategoryRequest(string Name, string? Description, Guid? ParentCategoryId = null);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CreateCertificationDefinitionRequest(
    string Name,
    string Issuer,
    string? Description,
    int? DefaultValidityMonths,
    int? RenewalLeadTimeMonths);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record RecordCertificationRequest(
    string Name,
    string Authority,
    string? LicenseNumber,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ExpiresAt,
    Guid? CertificationDefinitionId = null);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record RevokeRequest(string Reason);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CreateRoleRequirementRequest(string RoleTitle, string? JobFamily, string? Department);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record AddRequiredSkillRequest(Guid SkillId, string MinimumLevel, bool? IsMandatory = true);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record GapDetail(
    Guid SkillId,
    string SkillName,
    string Category,
    string RequiredLevel,
    string? ActualLevel,
    bool IsMissing,
    int LevelsBehind);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CreateLearningModuleRequest(
    string Title,
    string? Description,
    int EstimatedMinutes,
    string? ExternalProviderId,
    string? ContentUrl);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record LinkSkillToModuleRequest(Guid SkillId, string GrantedLevel);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record EnrollRequest(
    Guid ModuleId,
    bool? IsMandatory,
    DateTimeOffset? Deadline,
    Guid? TargetSkillId = null);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record ModuleRecommendation(Guid ModuleId, string Title, int EstimatedMinutes, string? ContentUrl);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record SkillRecommendation(
    Guid SkillId,
    string SkillName,
    string RequiredLevel,
    string? ActualLevel,
    bool IsMissing,
    IReadOnlyList<ModuleRecommendation> RecommendedModules);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record LearningRecommendationResult(
    Guid RoleRequirementId,
    string RoleTitle,
    IReadOnlyList<SkillRecommendation> Recommendations);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record GapAnalysisResult(
    Guid RoleRequirementId,
    Guid EmployeeId,
    string RoleTitle,
    IReadOnlyList<GapDetail> Gaps,
    int TotalRequiredSkills,
    int EmployeeSkillCount = 0,
    decimal ReadinessPercent = 0m);
