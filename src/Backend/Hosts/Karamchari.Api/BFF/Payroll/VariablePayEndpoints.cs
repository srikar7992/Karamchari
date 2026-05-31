using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.VariablePay;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Payroll;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class VariablePayEndpoints
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapVariablePayEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payroll/variable-pay").RequireAuthorization();

        group.MapPost("/", AllocateVariablePay).WithName("VariablePay.Allocate");
        group.MapGet("/{id:guid}", GetVariablePay).WithName("VariablePay.Get");
        group.MapGet("/", ListVariablePay).WithName("VariablePay.List");

        return app;
    }

    private static async Task<IResult> AllocateVariablePay(
        [FromBody] SubmitVariablePayRequest request,
        ClaimsPrincipal user,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        if (!Enum.TryParse<VariablePayType>(request.Type, out var type))
            return Results.BadRequest($"Invalid variable pay type: {request.Type}");

        var pay = VariablePayAllocation.Allocate(
            tenantId, request.EmployeeId, request.EmployeeName,
            type, request.Amount, TaxTreatment.LumpSum,
            request.PeriodName, null, 0, employeeId.ToString()!);

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

    private static VariablePayDto MapToDto(VariablePayAllocation p) =>
        new(p.Id, p.EmployeeId, p.EmployeeName, p.Type.ToString(),
            p.ProratedAmount, p.PayoutPeriodName ?? "", p.Status.ToString(), p.CreatedAtUtc);
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record VariablePayDto(
    Guid Id, Guid EmployeeId, string EmployeeName, string Type,
    decimal Amount, string PeriodName, string Status, DateTimeOffset CreatedAt);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record SubmitVariablePayRequest(
    Guid EmployeeId, string EmployeeName, string Type,
    decimal Amount, string PeriodName, string Description);
