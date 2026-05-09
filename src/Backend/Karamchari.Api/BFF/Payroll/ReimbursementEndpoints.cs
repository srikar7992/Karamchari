using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Api.Middleware;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Reimbursements;
using Karamchari.Payroll.Services.Reimbursements;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Karamchari.Api.BFF.Common;

namespace Karamchari.Api.BFF.Payroll;

public static class ReimbursementEndpoints
{
    public static WebApplication MapReimbursementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payroll/reimbursements").RequireAuthorization();

        group.MapPost("/", SubmitClaim).WithName("Reimbursement.Submit").WithIdempotency();
        group.MapGet("/{id:guid}", GetClaim).WithName("Reimbursement.Get");
        group.MapGet("/", ListClaims).WithName("Reimbursement.List");
        group.MapGet("/my", GetMyClaims).WithName("Reimbursement.My");
        group.MapPost("/{id:guid}/approve", ApproveClaim).WithName("Reimbursement.Approve").WithIdempotency();
        group.MapPost("/{id:guid}/reject", RejectClaim).WithName("Reimbursement.Reject").WithIdempotency();
        group.MapPost("/{id:guid}/clawback", ClawbackClaim).WithName("Reimbursement.Clawback").WithIdempotency();

        return app;
    }

    private static async Task<IResult> SubmitClaim(
        [FromBody] SubmitReimbursementRequest request,
        ClaimsPrincipal user,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        if (!Enum.TryParse<ReimbursementCategory>(request.Category, out var category))
            return Results.BadRequest($"Invalid category: {request.Category}");

        var existingHashes = await db.Set<ReimbursementClaim>()
            .AsNoTracking()
            .Where(c => c.EmployeeId == employeeId)
            .Select(c => c.AttachmentHash)
            .ToListAsync(ct);

        try
        {
            var claim = ReimbursementClaim.SubmitWithPolicy(
                tenantId, employeeId.Value, user.Identity?.Name ?? employeeId.ToString()!,
                category, request.Description, request.ClaimedAmount, 
                request.ExpenseDate, employeeId.ToString()!, existingHashes,
                request.AttachmentBlobPath, request.AttachmentFileName, request.AttachmentHash);

            db.Set<ReimbursementClaim>().Add(claim);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/payroll/reimbursements/{claim.Id}", MapToDto(claim));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetClaim(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var claim = await db.Set<ReimbursementClaim>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return claim is null ? Results.NotFound() : Results.Ok(MapToDto(claim));
    }

    private static async Task<IResult> ListClaims(
        PayrollDbContext db,
        [FromQuery] string? status,
        [FromQuery] Guid? employeeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.Set<ReimbursementClaim>().AsNoTracking();

        if (employeeId.HasValue)
            query = query.Where(c => c.EmployeeId == employeeId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReimbursementStatus>(status, out var s))
            query = query.Where(c => c.Status == s);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Results.Ok(new { Items = items.Select(MapToDto), Total = total, Page = page });
    }

    private static async Task<IResult> GetMyClaims(
        ClaimsPrincipal user,
        PayrollDbContext db,
        [FromQuery] string? status,
        CancellationToken ct = default)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var query = db.Set<ReimbursementClaim>().AsNoTracking()
            .Where(c => c.EmployeeId == employeeId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReimbursementStatus>(status, out var s))
            query = query.Where(c => c.Status == s);

        var items = await query.OrderByDescending(c => c.CreatedAtUtc).ToListAsync(ct);
        return Results.Ok(items.Select(MapToDto));
    }

    private static async Task<IResult> ApproveClaim(
        Guid id,
        [FromBody] decimal approvedAmount,
        ClaimsPrincipal user,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var claim = await db.Set<ReimbursementClaim>().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (claim is null) return Results.NotFound();

        var approvedBy = user.GetEmployeeIdString();
        if (approvedBy is null) return Results.Unauthorized();

        claim.Approve(approvedBy, approvedAmount);

        await db.SaveChangesAsync(ct);

        await bus.Publish(new ReimbursementApprovedIntegrationEvent
        {
            ClaimId = claim.Id,
            TenantId = claim.TenantId,
            EmployeeId = claim.EmployeeId,
            ApprovedAmount = claim.ApprovedAmount,
            Category = claim.Category.ToString(),
            OccurredOnUtc = DateTimeOffset.UtcNow
        }, ct);

        return Results.Ok(MapToDto(claim));
    }

    private static async Task<IResult> RejectClaim(
        Guid id,
        [FromBody] string reason,
        ClaimsPrincipal user,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var claim = await db.Set<ReimbursementClaim>().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (claim is null) return Results.NotFound();

        var rejectedBy = user.GetEmployeeIdString();
        if (rejectedBy is null) return Results.Unauthorized();
        
        claim.Reject(rejectedBy, reason);

        await db.SaveChangesAsync(ct);
        return Results.Ok(MapToDto(claim));
    }

    private static async Task<IResult> ClawbackClaim(
        Guid id,
        [FromBody] string reason,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var claim = await db.Set<ReimbursementClaim>()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (claim is null) return Results.NotFound();

        claim.Clawback(reason);
        await db.SaveChangesAsync(ct);
        return Results.Ok(MapToDto(claim));
    }

    private static ReimbursementClaimDto MapToDto(ReimbursementClaim c) =>
        new(c.Id, c.EmployeeId, c.EmployeeName, c.Category.ToString(),
            c.Status.ToString(), c.ClaimedAmount, c.ApprovedAmount,
            c.PolicyLimit, c.Description, c.ExpenseDate,
            c.FraudIndicator.ToString(), c.IsClawedBack, c.CreatedAtUtc);
}
