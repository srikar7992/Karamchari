// -----------------------------------------------------------------------
// <copyright file="OnboardingEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Onboarding.Domain;
using Karamchari.Onboarding.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Onboarding;

public static class OnboardingEndpoints
{
    public static WebApplication MapOnboardingEndpoints(this WebApplication app)
    {
        var cases = app.MapGroup("/api/v1/onboarding/cases").RequireAuthorization();
        cases.MapPost("/", InitiateCase).WithName("Onboarding.Cases.Initiate");
        cases.MapGet("/", ListCases).WithName("Onboarding.Cases.List");
        cases.MapGet("/{caseId:guid}", GetCase).WithName("Onboarding.Cases.Get");
        cases.MapPost("/{caseId:guid}/start", StartCase).WithName("Onboarding.Cases.Start");
        cases.MapPost("/{caseId:guid}/cancel", CancelCase).WithName("Onboarding.Cases.Cancel");
        cases.MapPost("/{caseId:guid}/tasks/{taskId:guid}/complete", CompleteTask).WithName("Onboarding.Cases.CompleteTask");
        cases.MapPost("/{caseId:guid}/tasks/{taskId:guid}/skip", SkipTask).WithName("Onboarding.Cases.SkipTask");
        cases.MapPost("/{caseId:guid}/documents", AddDocument).WithName("Onboarding.Cases.AddDocument");
        cases.MapPost("/{caseId:guid}/documents/{documentId:guid}/submit", SubmitDocument).WithName("Onboarding.Cases.SubmitDocument");
        cases.MapPost("/{caseId:guid}/documents/{documentId:guid}/verify", VerifyDocument).WithName("Onboarding.Cases.VerifyDocument");
        cases.MapPost("/{caseId:guid}/documents/{documentId:guid}/reject", RejectDocument).WithName("Onboarding.Cases.RejectDocument");

        var templates = app.MapGroup("/api/v1/onboarding/templates").RequireAuthorization();
        templates.MapGet("/", ListTemplates).WithName("Onboarding.Templates.List");
        templates.MapPost("/", CreateTemplate).WithName("Onboarding.Templates.Create");
        templates.MapGet("/{templateId:guid}", GetTemplate).WithName("Onboarding.Templates.Get");
        templates.MapPost("/{templateId:guid}/tasks", AddTemplateTask).WithName("Onboarding.Templates.AddTask");
        templates.MapPost("/{templateId:guid}/deactivate", DeactivateTemplate).WithName("Onboarding.Templates.Deactivate");

        return app;
    }

    // ── Cases ──

    private static async Task<IResult> InitiateCase(
        [FromBody] InitiateCaseRequest req,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        OnboardingTemplate? template = null;
        if (req.TemplateId.HasValue)
        {
            template = await db.Templates
                .Include(t => t.Tasks)
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == new OnboardingTemplateId(req.TemplateId.Value), ct);
            if (template is null) return Results.NotFound("Template not found.");
        }

        var @case = OnboardingCase.Initiate(
            tenantId,
            req.EmployeeId,
            req.EmployeeName,
            req.CaseType,
            req.ExpectedStartDate,
            req.Department,
            req.Role,
            template?.Id);

        if (template is not null)
        {
            template.ApplyTo(@case);
            @case.Start();
        }

        db.Cases.Add(@case);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/onboarding/cases/{@case.Id.Value}", new
        {
            @case.Id,
            @case.Status,
            ProgressPercent = @case.ProgressPercent(),
        });
    }

    private static async Task<IResult> ListCases(
        [FromQuery] Guid? employeeId,
        [FromQuery] string? status,
        [FromQuery] string? caseType,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var query = db.Cases.Where(c => c.TenantId == tenantId);

        if (employeeId.HasValue)
            query = query.Where(c => c.EmployeeId == employeeId.Value);

        if (Enum.TryParse<OnboardingCaseStatus>(status, ignoreCase: true, out var parsedStatus))
            query = query.Where(c => c.Status == parsedStatus);

        if (Enum.TryParse<OnboardingCaseType>(caseType, ignoreCase: true, out var parsedType))
            query = query.Where(c => c.CaseType == parsedType);

        var results = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.EmployeeId,
                c.EmployeeName,
                c.CaseType,
                c.Status,
                c.ExpectedStartDate,
                c.CompletedAt,
                c.Department,
                c.Role,
                c.CreatedAt,
            })
            .ToListAsync(ct);

        return Results.Ok(results);
    }

    private static async Task<IResult> GetCase(
        Guid caseId,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var @case = await db.Cases
            .Include(c => c.Tasks)
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == new OnboardingCaseId(caseId), ct);

        if (@case is null) return Results.NotFound();

        return Results.Ok(new
        {
            @case.Id,
            @case.EmployeeId,
            @case.EmployeeName,
            @case.CaseType,
            @case.Status,
            @case.ExpectedStartDate,
            @case.CompletedAt,
            @case.Department,
            @case.Role,
            @case.CreatedAt,
            ProgressPercent = @case.ProgressPercent(),
            Tasks = @case.Tasks.Select(t => new
            {
                t.Id,
                t.Title,
                t.Category,
                t.Status,
                t.AssignedToEmployeeId,
                t.DueDate,
                t.CompletedAt,
                t.CompletionNotes,
                t.SkipReason,
                IsOverdue = t.IsOverdue,
            }),
            Documents = @case.Documents.Select(d => new
            {
                d.Id,
                d.DocumentType,
                d.Status,
                d.FileName,
                d.SubmittedAt,
                d.VerifiedAt,
                d.RejectionReason,
            }),
        });
    }

    private static async Task<IResult> StartCase(
        Guid caseId,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var @case = await db.Cases.FirstOrDefaultAsync(
            c => c.TenantId == tenantId && c.Id == new OnboardingCaseId(caseId), ct);
        if (@case is null) return Results.NotFound();

        try
        {
            @case.Start();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { @case.Status });
    }

    private static async Task<IResult> CancelCase(
        Guid caseId,
        [FromBody] CancelCaseRequest req,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var @case = await db.Cases.FirstOrDefaultAsync(
            c => c.TenantId == tenantId && c.Id == new OnboardingCaseId(caseId), ct);
        if (@case is null) return Results.NotFound();

        try
        {
            @case.Cancel(req.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> CompleteTask(
        Guid caseId,
        Guid taskId,
        [FromBody] CompleteTaskRequest req,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var @case = await db.Cases
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == new OnboardingCaseId(caseId), ct);
        if (@case is null) return Results.NotFound();

        try
        {
            @case.CompleteTask(new OnboardingTaskId(taskId), req.CompletedByEmployeeId ?? employeeId.Value, req.Notes);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { @case.Status, ProgressPercent = @case.ProgressPercent() });
    }

    private static async Task<IResult> SkipTask(
        Guid caseId,
        Guid taskId,
        [FromBody] SkipTaskRequest req,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var @case = await db.Cases
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == new OnboardingCaseId(caseId), ct);
        if (@case is null) return Results.NotFound();

        try
        {
            @case.SkipTask(new OnboardingTaskId(taskId), req.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { ProgressPercent = @case.ProgressPercent() });
    }

    private static async Task<IResult> AddDocument(
        Guid caseId,
        [FromBody] AddDocumentRequest req,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var @case = await db.Cases
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == new OnboardingCaseId(caseId), ct);
        if (@case is null) return Results.NotFound();

        try
        {
            var doc = @case.AddRequiredDocument(req.DocumentType);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/onboarding/cases/{caseId}/documents/{doc.Id.Value}", new { doc.Id, doc.DocumentType, doc.Status });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }
    }

    private static async Task<IResult> SubmitDocument(
        Guid caseId,
        Guid documentId,
        [FromBody] SubmitDocumentRequest req,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var @case = await db.Cases
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == new OnboardingCaseId(caseId), ct);
        if (@case is null) return Results.NotFound();

        try
        {
            @case.SubmitDocument(new OnboardingDocumentId(documentId), req.FileName, req.StorageReference);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> VerifyDocument(
        Guid caseId,
        Guid documentId,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var @case = await db.Cases
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == new OnboardingCaseId(caseId), ct);
        if (@case is null) return Results.NotFound();

        try
        {
            @case.VerifyDocument(new OnboardingDocumentId(documentId), employeeId.Value.ToString());
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> RejectDocument(
        Guid caseId,
        Guid documentId,
        [FromBody] RejectDocumentRequest req,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var @case = await db.Cases
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == new OnboardingCaseId(caseId), ct);
        if (@case is null) return Results.NotFound();

        try
        {
            @case.RejectDocument(new OnboardingDocumentId(documentId), req.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ── Templates ──

    private static async Task<IResult> ListTemplates(
        [FromQuery] string? caseType,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var query = db.Templates.Where(t => t.TenantId == tenantId && t.IsActive);

        if (Enum.TryParse<OnboardingCaseType>(caseType, ignoreCase: true, out var parsed))
            query = query.Where(t => t.CaseType == parsed);

        var results = await query
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name, t.CaseType, t.Department, t.Role, t.IsActive, t.CreatedAt })
            .ToListAsync(ct);

        return Results.Ok(results);
    }

    private static async Task<IResult> CreateTemplate(
        [FromBody] CreateTemplateRequest req,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var template = OnboardingTemplate.Create(tenantId, req.Name, req.CaseType, req.Department, req.Role);
        db.Templates.Add(template);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/onboarding/templates/{template.Id.Value}", new { template.Id, template.Name });
    }

    private static async Task<IResult> GetTemplate(
        Guid templateId,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var template = await db.Templates
            .Include(t => t.Tasks)
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == new OnboardingTemplateId(templateId), ct);

        if (template is null) return Results.NotFound();

        return Results.Ok(new
        {
            template.Id,
            template.Name,
            template.CaseType,
            template.Department,
            template.Role,
            template.IsActive,
            Tasks = template.Tasks.Select(t => new
            {
                t.Id,
                t.Title,
                t.Category,
                t.DaysFromStartDue,
                t.Description,
            }),
        });
    }

    private static async Task<IResult> AddTemplateTask(
        Guid templateId,
        [FromBody] AddTemplateTaskRequest req,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var template = await db.Templates
            .Include(t => t.Tasks)
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == new OnboardingTemplateId(templateId), ct);

        if (template is null) return Results.NotFound();

        template.AddTask(req.Title, req.Category, req.DaysFromStartDue, req.Description);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeactivateTemplate(
        Guid templateId,
        ClaimsPrincipal user,
        OnboardingDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var template = await db.Templates.FirstOrDefaultAsync(
            t => t.TenantId == tenantId && t.Id == new OnboardingTemplateId(templateId), ct);

        if (template is null) return Results.NotFound();

        template.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ── Request records ──

    private sealed record InitiateCaseRequest(
        Guid EmployeeId,
        string EmployeeName,
        OnboardingCaseType CaseType,
        DateTimeOffset ExpectedStartDate,
        string? Department,
        string? Role,
        Guid? TemplateId);

    private sealed record CancelCaseRequest(string Reason);

    private sealed record CompleteTaskRequest(Guid? CompletedByEmployeeId, string? Notes);

    private sealed record SkipTaskRequest(string Reason);

    private sealed record AddDocumentRequest(DocumentType DocumentType);

    private sealed record SubmitDocumentRequest(string FileName, string StorageReference);

    private sealed record RejectDocumentRequest(string Reason);

    private sealed record CreateTemplateRequest(
        string Name,
        OnboardingCaseType CaseType,
        string? Department,
        string? Role);

    private sealed record AddTemplateTaskRequest(
        string Title,
        TaskCategory Category,
        int DaysFromStartDue,
        string? Description);
}
