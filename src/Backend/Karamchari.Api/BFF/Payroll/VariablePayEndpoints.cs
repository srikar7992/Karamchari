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

        group.MapPost("/", AllocateVariablePay).WithName("VariablePay.Allocate");
        group.MapGet("/{id:guid}", GetAllocation).WithName("VariablePay.Get");
        group.MapGet("/", ListAllocations).WithName("VariablePay.List");
        group.MapPost("/{id:guid}/approve", ApproveAllocation).WithName("VariablePay.Approve");
        group.MapPost("/{id:guid}/schedule", ScheduleAllocation).WithName("VariablePay.Schedule");
        group.MapPost("/{id:guid}/clawback", ClawbackAllocation).WithName("VariablePay.Clawback");

        return app;
    }

    private static async Task<IResult> AllocateVariablePay(
        [FromBody] AllocateVariablePayRequest request,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var tenantId = user.GetTenantId(httpRequest);
        var allocatedBy = user.GetEmployeeIdString(httpRequest);

        if (!Enum.TryParse<VariablePayType>(request.VariablePayType, out var vpType))
            return Results.BadRequest($"Invalid type: {request.VariablePayType}");

        if (!Enum.TryParse<TaxTreatment>(request.TaxTreatment, out var taxTreatment))
            return Results.BadRequest($"Invalid tax treatment: {request.TaxTreatment}");

        var allocation = VariablePayAllocation.Allocate(
            tenantId, request.EmployeeId, request.EmployeeName,
            vpType, request.AllocatedAmount, taxTreatment,
            request.PerformancePeriod, request.ScheduledPayoutDate,
            request.ClawbackWindowMonths, allocatedBy);

        db.Set<VariablePayAllocation>().Add(allocation);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/payroll/variable-pay/{allocation.Id}", MapToDto(allocation));
    }

    private static async Task<IResult> GetAllocation(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var allocation = await db.Set<VariablePayAllocation>()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        return allocation is null ? Results.NotFound() : Results.Ok(MapToDto(allocation));
    }

    private static async Task<IResult> ListAllocations(
        PayrollDbContext db,
        [FromQuery] Guid? employeeId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.Set<VariablePayAllocation>().AsNoTracking();

        if (employeeId.HasValue)
            query = query.Where(a => a.EmployeeId == employeeId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<VariablePayStatus>(status, out var s))
            query = query.Where(a => a.Status == s);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return Results.Ok(new { Items = items.Select(MapToDto), Total = total, Page = page });
    }

    private static async Task<IResult> ApproveAllocation(
        Guid id,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var allocation = await db.Set<VariablePayAllocation>()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (allocation is null) return Results.NotFound();

        var approvedBy = user.GetEmployeeIdString(httpRequest);
        allocation.Approve(approvedBy);
        await db.SaveChangesAsync(ct);

        await bus.Publish(new VariablePayApprovedIntegrationEvent
        {
            AllocationId = allocation.Id,
            TenantId = allocation.TenantId,
            EmployeeId = allocation.EmployeeId,
            VariablePayType = allocation.Type.ToString(),
            Amount = allocation.ProratedAmount,
            PayoutPeriodName = allocation.PayoutPeriodName,
            OccurredOnUtc = DateTimeOffset.UtcNow
        }, ct);

        return Results.Ok(MapToDto(allocation));
    }

    private static async Task<IResult> ScheduleAllocation(
        Guid id,
        [FromBody] string payoutPeriodName,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var allocation = await db.Set<VariablePayAllocation>()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (allocation is null) return Results.NotFound();

        allocation.Schedule(payoutPeriodName);
        await db.SaveChangesAsync(ct);
        return Results.Ok(MapToDto(allocation));
    }

    private static async Task<IResult> ClawbackAllocation(
        Guid id,
        [FromBody] string reason,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var allocation = await db.Set<VariablePayAllocation>()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (allocation is null) return Results.NotFound();

        allocation.Clawback(reason);
        await db.SaveChangesAsync(ct);
        return Results.Ok(MapToDto(allocation));
    }

    private static VariablePayAllocationDto MapToDto(VariablePayAllocation a) =>
        new(a.Id, a.EmployeeId, a.EmployeeName, a.Type.ToString(),
            a.Status.ToString(), a.AllocatedAmount, a.ProratedAmount,
            a.PaidAmount, a.PayoutPeriodName, a.IsClawedBack, a.CreatedAtUtc);
}
