// -----------------------------------------------------------------------
// <copyright file="EmployeeEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.Api.Validation;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Multitenancy;
using Karamchari.HR.Contracts.Employees;
using Karamchari.HR.Domain.Employees;
using Karamchari.HR.Persistence;
using MassTransit;
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

        group.MapPost("/", OnboardEmployee)
            .WithName("Employee.Onboard")
            .WithValidation<OnboardEmployeeCommand>();

        group.MapGet("/", GetEmployees)
            .WithName("Employee.List");

        group.MapGet("/{id:guid}", GetEmployee)
            .WithName("Employee.Get");

        group.MapPut("/{id:guid}", UpdateEmployee)
            .WithName("Employee.Update")
            .WithValidation<UpdateEmployeeCommand>();

        group.MapDelete("/{id:guid}", DeleteEmployee)
            .WithName("Employee.Delete");

        group.MapGet("/{id:guid}/history", GetEmployeeHistory).WithName("Employee.History");
        group.MapPost("/{id:guid}/transfer", TransferEmployee).WithName("Employee.Transfer");
        group.MapPost("/{id:guid}/terminate", TerminateEmployee).WithName("Employee.Terminate");

        return app;
    }

    private static async Task<IResult> OnboardEmployee(
        [FromBody] OnboardEmployeeCommand command,
        IEmployeeService employeeService,
        CancellationToken ct)
    {
        try
        {
            var id = await employeeService.OnboardEmployeeAsync(command, ct);
            return Results.Created($"/api/v1/hr/employees/{id}", new { id });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetEmployees(
        IEmployeeService employeeService,
        CancellationToken ct)
    {
        var employees = await employeeService.GetEmployeesAsync(ct);
        return Results.Ok(employees);
    }

    private static async Task<IResult> GetEmployee(
        Guid id,
        IEmployeeService employeeService,
        CancellationToken ct)
    {
        var employee = await employeeService.GetEmployeeByIdAsync(id, ct);
        if (employee == null) return Results.NotFound();
        return Results.Ok(employee);
    }

    private static async Task<IResult> UpdateEmployee(
        Guid id,
        [FromBody] UpdateEmployeeCommand command,
        IEmployeeService employeeService,
        CancellationToken ct)
    {
        try
        {
            await employeeService.UpdateEmployeeAsync(id, command, ct);
            return Results.Ok();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteEmployee(
        Guid id,
        IEmployeeService employeeService,
        CancellationToken ct)
    {
        try
        {
            await employeeService.DeleteEmployeeAsync(id, ct);
            return Results.Ok();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.ToString() });
        }
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

    private static async Task<IResult> TerminateEmployee(
        Guid id,
        [FromBody] TerminateEmployeeRequest req,
        ClaimsPrincipal user,
        HRDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var employee = await db.Employees.FindAsync([id], ct);
        if (employee is null) return Results.NotFound();
        if (employee.Status == EmploymentStatus.Terminated)
            return Results.Conflict(new { error = "Employee already terminated." });

        var actorId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var terminatedOn = req.TerminatedOn ?? DateTimeOffset.UtcNow;

        employee.Terminate(terminatedOn, actorId);
        await db.SaveChangesAsync(ct);

        await bus.Publish(new EmployeeTerminatedIntegrationEvent(
            employee.Id,
            employee.TenantId,
            employee.EmployeeNumber,
            DateOnly.FromDateTime(terminatedOn.UtcDateTime),
            req.Reason), ct);

        return Results.Ok(new { employee.Status, TerminatedOn = terminatedOn });
    }
}

public record EmployeeHistoryDto(
    string Type, string Previous, string New,
    DateTimeOffset EffectiveFrom, DateTimeOffset? EffectiveTo,
    Guid ChangedBy, DateTimeOffset LoggedAt);

public record TransferRequest(Guid NewDepartmentId, DateTimeOffset EffectiveFrom);
public record TerminateEmployeeRequest(string Reason, DateTimeOffset? TerminatedOn = null);
