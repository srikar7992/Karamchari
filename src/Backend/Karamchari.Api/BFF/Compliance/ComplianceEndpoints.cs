using System.Security.Claims;
using Karamchari.Api.BFF.Common;
using Karamchari.Payroll.Services.Compliance;
using Microsoft.AspNetCore.Mvc;

namespace Karamchari.Api.BFF.Compliance;

public static class ComplianceEndpoints
{
    public static WebApplication MapComplianceEndpoints(this WebApplication app)
    {
        var compliance = app.MapGroup("/api/compliance").RequireAuthorization();

        compliance.MapGet("/epf/{payrollRunId}", DownloadEpfEcr);
        compliance.MapGet("/esic/{payrollRunId}", DownloadEsicMonthly);
        compliance.MapGet("/tds/{payrollRunId}", DownloadTdsForm24Q);
        compliance.MapPost("/mark-filed", MarkAsFiled);
        compliance.MapGet("/health", GetComplianceHealth);

        var admin = app.MapGroup("/api/admin").RequireAuthorization();
        admin.MapGet("/declarations/pending", GetPendingDeclarations);
        admin.MapGet("/declarations/{id}/document", GetDeclarationDocument);
        admin.MapPut("/declarations/{id}/approve", ApproveDeclaration);
        admin.MapPut("/declarations/{id}/reject", RejectDeclaration);

        return app;
    }

    private static async Task<IResult> DownloadEpfEcr(
        Guid payrollRunId,
        IComplianceService complianceService)
    {
        var (fileName, content) = await complianceService.GetEpfEcrAsync(payrollRunId);
        return Results.File(content, "text/plain", fileName);
    }

    private static async Task<IResult> DownloadEsicMonthly(
        Guid payrollRunId,
        IComplianceService complianceService)
    {
        var (fileName, content) = await complianceService.GetEsicMonthlyAsync(payrollRunId);
        return Results.File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static async Task<IResult> DownloadTdsForm24Q(
        Guid payrollRunId,
        IComplianceService complianceService)
    {
        var (fileName, content) = await complianceService.GetTdsForm24QAsync(payrollRunId);
        return Results.File(content, "text/plain", fileName);
    }

    private static async Task<IResult> MarkAsFiled(
        MarkFiledRequest request,
        IComplianceService complianceService,
        ClaimsPrincipal user)
    {
        var filedBy = user.Identity?.Name ?? "Admin";
        await complianceService.MarkAsFiledAsync(request.RunId, request.Type, request.ReferenceNumber, filedBy);
        return Results.Ok();
    }

    private static async Task<IResult> GetComplianceHealth(IComplianceService complianceService)
    {
        var health = await complianceService.GetComplianceHealthAsync();
        return Results.Ok(health);
    }

    private static async Task<IResult> GetPendingDeclarations(Karamchari.Payroll.Services.Statutory.IITDeclarationRepository repo)
    {
        var items = await repo.GetPendingReviewAsync();
        return Results.Ok(items.Select(x => new
        {
            x.Id,
            x.EmployeeId,
            x.Category,
            x.ClaimedAmount,
            x.SubmittedAt,
            x.ProofUri
        }));
    }

    private static async Task<IResult> GetDeclarationDocument(
        Guid id, 
        Karamchari.Payroll.Services.Statutory.IITDeclarationRepository repo, 
        Karamchari.Payroll.Services.Declarations.IProofStorage storage)
    {
        var declaration = await repo.GetByIdAsync(id);
        if (declaration == null) return Results.NotFound();

        var stream = await storage.GetStreamAsync(declaration.ProofUri);
        var extension = Path.GetExtension(declaration.ProofUri).ToLowerInvariant();
        var contentType = extension == ".pdf" ? "application/pdf" : "image/jpeg";
        return Results.File(stream, contentType);
    }

    private static async Task<IResult> ApproveDeclaration(
        Guid id, 
        [FromBody] decimal amount, 
        Karamchari.Payroll.Services.Declarations.ITDeclarationService service)
    {
        var approver = "HR_Admin_01";
        await service.ApproveAsync(id, amount, approver);
        return Results.NoContent();
    }

    private static async Task<IResult> RejectDeclaration(
        Guid id, 
        [FromBody] string reason, 
        Karamchari.Payroll.Services.Declarations.ITDeclarationService service)
    {
        var rejectedBy = "HR_Admin_01";
        await service.RejectAsync(id, reason, rejectedBy);
        return Results.NoContent();
    }
}
