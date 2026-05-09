using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.VariablePay;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Payroll;

public static class VariablePayEndpoints
{
    public static WebApplication MapVariablePayEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payroll/variable-pay").RequireAuthorization();

        group.MapPost("/", SubmitVariablePay).WithName("VariablePay.Submit");
        group.MapGet("/{id:guid}", GetVariablePay).WithName("VariablePay.Get");
        group.MapGet("/", ListVariablePay).WithName("VariablePay.List");
        group.MapPost("/{id:guid}/approve", ApproveVariablePay).WithName("VariablePay.Approve");

        return app;
    }

    private static async Task<IResult> SubmitVariablePay(
        [FromBody] SubmitVariablePayRequest request,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var tenantIdStr = user.GetTenantId(httpRequest) ?? "00000000-0000-0000-0000-000000000000";
        var submittedBy = user.GetEmployeeIdString(httpRequest) ?? "system";
        var tenantId = Guid.Parse(tenantIdStr);

        if (!Enum.TryParse<VariablePayType>(request.Type, out var type))
            return Results.BadRequest($"Invalid variable pay type: {request.Type}");

        var pay = VariablePayAllocation.Allocate(
            tenantId, request.EmployeeId, request.EmployeeName,
            type, request.Amount, TaxTreatment.LumpSum,
            request.PeriodName, null, 0, submittedBy);

        db.Set<VariablePayAllocation>().Add(pay);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/payroll/variable-pay/{pay.Id}", MapToDto(pay));
    }

    private static async Task<IResult> GetVariablePay(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var pay = await db.Set<VariablePayAllocation>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return pay is null ? Results.NotFound() : Results.Ok(MapToDto(pay));
    }

    private static async Task<IResult> ListVariablePay(
        PayrollDbContext db,
        [FromQuery] string? periodName,
        [FromQuery] Guid? employeeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.Set<VariablePayAllocation>().AsNoTracking();

        if (employeeId.HasValue)
            query = query.Where(p => p.EmployeeId == employeeId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Results.Ok(new { Items = items.Select(MapToDto), Total = total, Page = page });
    }

    private static async Task<IResult> ApproveVariablePay(
        Guid id,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var pay = await db.Set<VariablePayAllocation>().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pay is null) return Results.NotFound();

        var approvedBy = user.GetEmployeeIdString(httpRequest) ?? "system";
        pay.Approve(approvedBy);

        await db.SaveChangesAsync(ct);

        await bus.Publish(new VariablePayApprovedIntegrationEvent
        {
            AllocationId = pay.Id,
            TenantId = pay.TenantId,
            EmployeeId = pay.EmployeeId,
            Amount = pay.ProratedAmount,
            VariablePayType = pay.Type.ToString(),
            PayoutPeriodName = pay.PayoutPeriodName,
            OccurredOnUtc = DateTimeOffset.UtcNow
        }, ct);

        return Results.Ok(MapToDto(pay));
    }

    private static VariablePayDto MapToDto(VariablePayAllocation p) =>
        new(p.Id, p.EmployeeId, p.EmployeeName, p.Type.ToString(),
            p.ProratedAmount, p.PayoutPeriodName ?? "", p.Status.ToString(), p.CreatedAtUtc);
}

public record VariablePayDto(
    Guid Id, Guid EmployeeId, string EmployeeName, string Type,
    decimal Amount, string PeriodName, string Status, DateTimeOffset CreatedAt);

public record SubmitVariablePayRequest(
    Guid EmployeeId, string EmployeeName, string Type,
    decimal Amount, string PeriodName, string Description);
