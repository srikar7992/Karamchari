using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.FnF;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Payroll;

public static class FnFEndpoints
{
    public static WebApplication MapFnFEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payroll/fnf").RequireAuthorization();

        group.MapPost("/", InitiateSettlement).WithName("FnF.Initiate");
        group.MapGet("/{id:guid}", GetSettlement).WithName("FnF.Get");
        group.MapGet("/", ListSettlements).WithName("FnF.List");
        group.MapPost("/{id:guid}/submit", SubmitForApproval).WithName("FnF.Submit");
        group.MapPost("/{id:guid}/approve", ApproveSettlement).WithName("FnF.Approve");
        group.MapPost("/{id:guid}/disburse", DisburseSettlement).WithName("FnF.Disburse");
        group.MapPost("/{id:guid}/hold", PlaceOnHold).WithName("FnF.Hold");
        group.MapPost("/{id:guid}/release-hold", ReleaseHold).WithName("FnF.ReleaseHold");
        group.MapPost("/{id:guid}/reopen", Reopen).WithName("FnF.Reopen");

        return app;
    }

    private static async Task<IResult> InitiateSettlement(
        [FromBody] InitiateFnFRequest request,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var tenantId = user.GetTenantId(httpRequest);
        var initiatedBy = user.GetEmployeeIdString(httpRequest);

        if (!Enum.TryParse<FnFExitType>(request.ExitType, out var exitType))
            return Results.BadRequest($"Invalid exit type: {request.ExitType}");

        var settlement = FnFSettlement.Initiate(
            tenantId, request.EmployeeId, request.EmployeeName,
            exitType, request.LastWorkingDay, initiatedBy);

        db.Set<FnFSettlement>().Add(settlement);
        await db.SaveChangesAsync(ct);

        await bus.Publish(new FnFSettlementInitiatedIntegrationEvent
        {
            SettlementId = settlement.Id,
            TenantId = tenantId,
            EmployeeId = request.EmployeeId,
            ExitType = request.ExitType,
            LastWorkingDay = request.LastWorkingDay,
            OccurredOnUtc = DateTimeOffset.UtcNow
        }, ct);

        return Results.Created($"/api/v1/payroll/fnf/{settlement.Id}", MapToDto(settlement));
    }

    private static async Task<IResult> GetSettlement(
        Guid id,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var settlement = await db.Set<FnFSettlement>()
            .AsNoTracking()
            .Include(s => s.LineItems)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return settlement is null ? Results.NotFound() : Results.Ok(MapToDto(settlement));
    }

    private static async Task<IResult> ListSettlements(
        PayrollDbContext db,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.Set<FnFSettlement>().AsNoTracking();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<FnFStatus>(status, out var s))
            query = query.Where(x => x.Status == s);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.LineItems)
            .ToListAsync(ct);

        return Results.Ok(new { Items = items.Select(MapToDto), Total = total, Page = page, PageSize = pageSize });
    }

    private static async Task<IResult> SubmitForApproval(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var settlement = await db.Set<FnFSettlement>()
            .Include(s => s.LineItems)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (settlement is null) return Results.NotFound();

        settlement.SubmitForApproval();
        await db.SaveChangesAsync(ct);
        return Results.Ok(MapToDto(settlement));
    }

    private static async Task<IResult> ApproveSettlement(
        Guid id,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var settlement = await db.Set<FnFSettlement>()
            .Include(s => s.LineItems)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (settlement is null) return Results.NotFound();

        var approvedBy = user.GetEmployeeIdString(httpRequest);
        settlement.Approve(approvedBy);
        await db.SaveChangesAsync(ct);

        await bus.Publish(new FnFSettlementApprovedIntegrationEvent
        {
            SettlementId = settlement.Id,
            TenantId = settlement.TenantId,
            EmployeeId = settlement.EmployeeId,
            NetSettlementAmount = settlement.NetSettlementAmount,
            ApprovedBy = approvedBy,
            OccurredOnUtc = DateTimeOffset.UtcNow
        }, ct);

        return Results.Ok(MapToDto(settlement));
    }

    private static async Task<IResult> DisburseSettlement(
        Guid id,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var settlement = await db.Set<FnFSettlement>()
            .Include(s => s.LineItems)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (settlement is null) return Results.NotFound();

        await bus.Publish(new DisburseFnFCommand { SettlementId = id, TenantId = settlement.TenantId }, ct);
        return Results.Accepted();
    }

    private static async Task<IResult> PlaceOnHold(
        Guid id,
        [FromBody] string reason,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var settlement = await db.Set<FnFSettlement>()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (settlement is null) return Results.NotFound();

        settlement.PlaceOnLegalHold(reason);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> ReleaseHold(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var settlement = await db.Set<FnFSettlement>()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (settlement is null) return Results.NotFound();

        settlement.ReleaseHold();
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> Reopen(
        Guid id,
        [FromBody] string reason,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var settlement = await db.Set<FnFSettlement>()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (settlement is null) return Results.NotFound();

        settlement.Reopen(reason);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static FnFSettlementDto MapToDto(FnFSettlement s) =>
        new(s.Id, s.EmployeeId, s.EmployeeName, s.ExitType.ToString(),
            s.LastWorkingDay, s.Status.ToString(),
            s.TotalEarnings, s.TotalDeductions, s.NetSettlementAmount,
            s.LineItems.Select(l => new FnFLineItemDto(
                l.Type.ToString(), l.Description, l.Amount, l.IsDeduction, l.IsTaxable)).ToList(),
            s.CreatedAtUtc);
}
