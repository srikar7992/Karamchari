using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Arrears;
using Karamchari.Payroll.Services.Arrears;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Payroll;

public static class ArrearEndpoints
{
    public static WebApplication MapArrearEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payroll/arrears").RequireAuthorization();

        group.MapPost("/trigger", TriggerArrearCalculation).WithName("Arrear.Trigger");
        group.MapGet("/{id:guid}", GetArrear).WithName("Arrear.Get");
        group.MapGet("/", ListArrears).WithName("Arrear.List");
        group.MapPost("/{id:guid}/approve", ApproveArrear).WithName("Arrear.Approve");

        return app;
    }

    private static async Task<IResult> TriggerArrearCalculation(
        [FromBody] TriggerArrearCalculationCommand command,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        ArrearCalculationEngine engine,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var tenantId = user.GetTenantId(httpRequest);

        var request = new ArrearCalculationRequest(
            tenantId, command.EmployeeId, "",
            Enum.Parse<ArrearTriggerType>(command.TriggerType),
            command.TriggerReference,
            command.EffectiveFrom, command.EffectiveTo,
            command.InitiatedBy);

        var result = await engine.ComputeAsync(request, ct);

        if (result.PeriodDiffs.Count == 0)
            return Results.Ok(new { message = "No arrear differentials found." });

        db.Set<ArrearCalculation>().Add(result.Calculation);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/payroll/arrears/{result.Calculation.Id}",
            MapToDto(result.Calculation));
    }

    private static async Task<IResult> GetArrear(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var arrear = await db.Set<ArrearCalculation>()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        return arrear is null ? Results.NotFound() : Results.Ok(MapToDto(arrear));
    }

    private static async Task<IResult> ListArrears(
        PayrollDbContext db,
        [FromQuery] string? status,
        [FromQuery] Guid? employeeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.Set<ArrearCalculation>().AsNoTracking();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ArrearStatus>(status, out var s))
            query = query.Where(a => a.Status == s);

        if (employeeId.HasValue)
            query = query.Where(a => a.EmployeeId == employeeId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Results.Ok(new { Items = items.Select(MapToDto), Total = total, Page = page });
    }

    private static async Task<IResult> ApproveArrear(
        Guid id,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var arrear = await db.Set<ArrearCalculation>()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (arrear is null) return Results.NotFound();

        var approvedBy = user.GetEmployeeIdString(httpRequest);
        arrear.Approve(approvedBy);
        await db.SaveChangesAsync(ct);

        await bus.Publish(new ArrearCalculationApprovedIntegrationEvent
        {
            ArrearId = arrear.Id,
            TenantId = arrear.TenantId,
            EmployeeId = arrear.EmployeeId,
            TotalNetDelta = arrear.TotalNetDelta,
            TriggerType = arrear.TriggerType.ToString(),
            OccurredOnUtc = DateTimeOffset.UtcNow
        }, ct);

        return Results.Ok(MapToDto(arrear));
    }

    private static ArrearCalculationDto MapToDto(ArrearCalculation a) =>
        new(a.Id, a.EmployeeId, a.EmployeeName, a.TriggerType.ToString(),
            a.Status.ToString(), a.EffectiveFrom, a.EffectiveTo,
            a.TotalNetDelta, a.TotalGrossDelta,
            a.PeriodDiffs.Select(d => new ArrearPeriodDiffDto(
                d.PeriodName, d.OriginalGross, d.RecalculatedGross,
                d.GrossDelta, d.NetDelta, d.TdsDelta)).ToList());
}
