using System.Security.Claims;
using Karamchari.Core.Multitenancy;
using Karamchari.HR.Domain.Employees;
using Karamchari.HR.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Employee;

/// <summary>
/// Provides HR administration endpoints for employee management and history.
/// </summary>
public static class EmployeeEndpoints
{
    public static WebApplication MapEmployeeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/hr/employees").RequireAuthorization();

        group.MapGet("/{id:guid}/history", GetEmployeeHistory).WithName("Employee.History");
        group.MapPost("/{id:guid}/transfer", TransferEmployee).WithName("Employee.Transfer");

        return app;
    }

    private static async Task<IResult> GetEmployeeHistory(
        Guid id,
        HRDbContext db,
        CancellationToken ct)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .Include(e => e.History)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (employee == null) return Results.NotFound();

        var timeline = employee.History
            .OrderByDescending(h => h.EffectiveFrom)
            .Select(h => new EmployeeHistoryDto(
                h.Type.ToString(),
                h.PreviousValue,
                h.NewValue,
                h.EffectiveFrom,
                h.EffectiveTo,
                h.ChangedBy,
                h.CreatedAt))
            .ToList();

        return Results.Ok(timeline);
    }

    private static async Task<IResult> TransferEmployee(
        Guid id,
        [FromBody] TransferRequest request,
        ClaimsPrincipal user,
        HRDbContext db,
        CancellationToken ct)
    {
        var employee = await db.Employees.FindAsync([id], ct);
        if (employee == null) return Results.NotFound();

        var actorId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

        try
        {
            employee.TransferToDepartment(request.NewDepartmentId, request.EffectiveFrom, actorId);
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}

public record EmployeeHistoryDto(
    string Type, string Previous, string New,
    DateTimeOffset EffectiveFrom, DateTimeOffset? EffectiveTo,
    Guid ChangedBy, DateTimeOffset LoggedAt);

public record TransferRequest(Guid NewDepartmentId, DateTimeOffset EffectiveFrom);
