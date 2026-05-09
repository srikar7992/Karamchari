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
        group.MapPost("/{id:guid}/approve", ApproveSettlement).WithName("FnF.Approve");
        group.MapPost("/{id:guid}/disburse", DisburseSettlement).WithName("FnF.Disburse");

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
        var tenantIdStr = user.GetTenantId(httpRequest) ?? "00000000-0000-0000-0000-000000000000";
        var initiatedBy = user.GetEmployeeIdString(httpRequest) ?? "system";
        var tenantId = Guid.Parse(tenantIdStr);

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
            ExitType = exitType.ToString(),
            LastWorkingDay = request.LastWorkingDay,
            OccurredOnUtc = DateTimeOffset.UtcNow
        }, ct);

        return Results.Created($"/api/v1/payroll/fnf/{settlement.Id}", MapToDto(settlement));
    }

    private static async Task<IResult> GetSettlement(
        Guid id, PayrollDbContext db, CancellationToken ct)
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
        [FromQuery] Guid? employeeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.Set<FnFSettlement>().AsNoTracking();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<FnFStatus>(status, out var s))
            query = query.Where(x => x.Status == s);

        if (employeeId.HasValue)
            query = query.Where(x => x.EmployeeId == employeeId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Results.Ok(new { Items = items.Select(MapToDto), Total = total, Page = page });
    }

    private static async Task<IResult> ApproveSettlement(
        Guid id,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var settlement = await db.Set<FnFSettlement>().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (settlement is null) return Results.NotFound();

        var approvedBy = user.GetEmployeeIdString(httpRequest) ?? "system";
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
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        await bus.Publish(new DisburseFnFCommand { SettlementId = id }, ct);
        return Results.Accepted();
    }

    private static FnFDto MapToDto(FnFSettlement s) =>
        new(s.Id, s.EmployeeId, s.EmployeeName, s.ExitType.ToString(),
            s.Status.ToString(), s.NetSettlementAmount, s.LastWorkingDay, s.CreatedAtUtc);
}

public record FnFDto(
    Guid Id, Guid EmployeeId, string EmployeeName, string ExitType,
    string Status, decimal NetAmount, DateOnly LastWorkingDay, DateTimeOffset CreatedAt);

public record InitiateFnFRequest(
    Guid EmployeeId, string EmployeeName, string ExitType, DateOnly LastWorkingDay);
