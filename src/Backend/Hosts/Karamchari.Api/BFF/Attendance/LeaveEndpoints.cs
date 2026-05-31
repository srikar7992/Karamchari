using System.Security.Claims;
using Karamchari.Api.BFF.Common;
using Karamchari.Api.Middleware;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Attendance;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class LeaveEndpoints
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapLeaveEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/leaves").RequireAuthorization();

        group.MapPost("/request", CreateLeaveRequest).WithName("Leave.Request").WithIdempotency();
        group.MapGet("/my", GetMyLeaves).WithName("Leave.My");
        group.MapGet("/{id:guid}", GetLeaveRequest).WithName("Leave.Get");

        return app;
    }

    private static async Task<IResult> CreateLeaveRequest(
        [FromBody] CreateLeaveRequestRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        // 1. Domain Logic: Calculation (This should ideally be in a domain service, but simple here for demo)
        // In a real app, we'd call LeaveRequestService.CalculateActualLeaveDays
        var actualDays = (request.EndDate.ToDateTime(TimeOnly.MinValue) - request.StartDate.ToDateTime(TimeOnly.MinValue)).TotalDays + 1;

        try
        {
            var leave = LeaveRequest.Create(employeeId.Value, request.PolicyId, request.StartDate, request.EndDate, actualDays, request.Reason);

            db.Set<LeaveRequest>().Add(leave);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/leaves/{leave.Id}", leave);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetMyLeaves(
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var leaves = await db.Set<LeaveRequest>()
            .Where(l => l.EmployeeId == employeeId.Value)
            .OrderByDescending(l => l.RequestedOnUtc)
            .ToListAsync(ct);

        return Results.Ok(leaves);
    }

    private static async Task<IResult> GetLeaveRequest(
        Guid id,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var leave = await db.Set<LeaveRequest>().FindAsync([id], ct);
        return leave is null ? Results.NotFound() : Results.Ok(leave);
    }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CreateLeaveRequestRequest(Guid PolicyId, DateOnly StartDate, DateOnly EndDate, string? Reason);
