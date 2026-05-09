using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Corrections;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Payroll;

public static class CorrectionEndpoints
{
    public static WebApplication MapCorrectionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payroll/corrections").RequireAuthorization();

        group.MapPost("/", SubmitCorrection).WithName("Correction.Submit");
        group.MapGet("/{id:guid}", GetCorrection).WithName("Correction.Get");
        group.MapGet("/", ListCorrections).WithName("Correction.List");
        group.MapPost("/{id:guid}/submit", SubmitForApproval).WithName("Correction.SubmitApproval");
        group.MapPost("/{id:guid}/approve", ApproveCorrection).WithName("Correction.Approve");
        group.MapPost("/{id:guid}/reject", RejectCorrection).WithName("Correction.Reject");

        return app;
    }

    private static async Task<IResult> SubmitCorrection(
        [FromBody] SubmitCorrectionRequest request,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var tenantId = user.GetTenantId(httpRequest);
        var requestedBy = user.GetEmployeeIdString(httpRequest);

        if (!Enum.TryParse<CorrectionType>(request.CorrectionType, out var corrType))
            return Results.BadRequest($"Invalid correction type: {request.CorrectionType}");

        if (!Enum.TryParse<CorrectionScope>(request.CorrectionScope, out var corrScope))
            return Results.BadRequest($"Invalid correction scope: {request.CorrectionScope}");

        // Idempotency check: duplicate correction for same employee+period+type
        var idempotencyKey = $"{tenantId}:{request.EmployeeId}:{request.AffectedYear}:{request.AffectedMonth}:{request.CorrectionType}";
        var duplicate = await db.Set<PayrollCorrection>()
            .AnyAsync(c => c.IdempotencyKey == idempotencyKey
                && c.Status != CorrectionStatus.Rejected
                && c.Status != CorrectionStatus.Cancelled, ct);

        if (duplicate)
            return Results.Conflict(new { message = "A correction for the same employee, period, and type already exists." });

        var correction = PayrollCorrection.Create(
            tenantId, request.EmployeeId, request.EmployeeName,
            corrType, corrScope,
            request.AffectedPeriodName, request.AffectedYear, request.AffectedMonth,
            request.ChangeDescription, request.ChangeDetails,
            request.AfterBankDisbursement, request.AfterTaxFiling,
            request.AfterEmployeeExit, requestedBy);

        db.Set<PayrollCorrection>().Add(correction);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/payroll/corrections/{correction.Id}", MapToDto(correction));
    }

    private static async Task<IResult> GetCorrection(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var correction = await db.Set<PayrollCorrection>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return correction is null ? Results.NotFound() : Results.Ok(MapToDto(correction));
    }

    private static async Task<IResult> ListCorrections(
        PayrollDbContext db,
        [FromQuery] string? status,
        [FromQuery] Guid? employeeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.Set<PayrollCorrection>().AsNoTracking();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CorrectionStatus>(status, out var s))
            query = query.Where(c => c.Status == s);

        if (employeeId.HasValue)
            query = query.Where(c => c.EmployeeId == employeeId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return Results.Ok(new { Items = items.Select(MapToDto), Total = total, Page = page });
    }

    private static async Task<IResult> SubmitForApproval(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var correction = await db.Set<PayrollCorrection>()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (correction is null) return Results.NotFound();

        correction.SubmitForApproval();
        await db.SaveChangesAsync(ct);
        return Results.Ok(MapToDto(correction));
    }

    private static async Task<IResult> ApproveCorrection(
        Guid id,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var correction = await db.Set<PayrollCorrection>()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (correction is null) return Results.NotFound();

        var approvedBy = user.GetEmployeeIdString(httpRequest);
        correction.Approve(approvedBy);
        await db.SaveChangesAsync(ct);

        await bus.Publish(new CorrectionApprovedIntegrationEvent
        {
            CorrectionId = correction.Id,
            TenantId = correction.TenantId,
            EmployeeId = correction.EmployeeId,
            CorrectionType = correction.Type.ToString(),
            AffectedPeriodName = correction.AffectedPeriodName,
            CorrectionScope = correction.Scope.ToString(),
            OccurredOnUtc = DateTimeOffset.UtcNow
        }, ct);

        return Results.Ok(MapToDto(correction));
    }

    private static async Task<IResult> RejectCorrection(
        Guid id,
        [FromBody] string reason,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var correction = await db.Set<PayrollCorrection>()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (correction is null) return Results.NotFound();

        correction.Reject(user.GetEmployeeIdString(httpRequest), reason);
        await db.SaveChangesAsync(ct);
        return Results.Ok(MapToDto(correction));
    }

    private static PayrollCorrectionDto MapToDto(PayrollCorrection c) =>
        new(c.Id, c.EmployeeId, c.EmployeeName, c.Type.ToString(),
            c.Scope.ToString(), c.Status.ToString(),
            c.AffectedPeriodName, c.ChangeDescription,
            c.DifferentialAmount, c.CreatedAtUtc);
}
