using System.Security.Claims;
using Karamchari.Api.BFF.Common;
using Karamchari.Api.Middleware;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.TimeAttendance.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Attendance;

public static class LeaveEndpoints
{
    public static WebApplication MapLeaveEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/leaves").RequireAuthorization();

        group.MapPost("/request", CreateLeaveRequest).WithName("Leave.Request").WithIdempotency();
        group.MapPost("/request/halfday", CreateHalfDayRequest).WithName("Leave.HalfDay").WithIdempotency();
        group.MapPost("/request/hourly", CreateHourlyRequest).WithName("Leave.Hourly").WithIdempotency();
        group.MapPost("/{id:guid}/submit", SubmitLeaveRequest).WithName("Leave.Submit");
        group.MapPost("/{id:guid}/cancel", CancelLeaveRequest).WithName("Leave.Cancel");
        group.MapPost("/{id:guid}/withdraw", WithdrawLeaveRequest).WithName("Leave.Withdraw");
        group.MapGet("/my", GetMyLeaves).WithName("Leave.My");
        group.MapGet("/{id:guid}", GetLeaveRequest).WithName("Leave.Get");
        group.MapGet("/balance/{policyId:guid}", GetBalance).WithName("Leave.Balance");
        group.MapGet("/balance/history/{policyId:guid}", GetBalanceHistory).WithName("Leave.BalanceHistory");

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

        var policy = await db.LeavePolicies.FindAsync([request.PolicyId], ct);
        if (policy is null) return Results.NotFound("Policy not found.");

        var holidays = await db.HolidayCalendars
            .Where(c => c.TenantId == tenantId)
            .SelectMany(c => c.Holidays)
            .Select(h => h.Date)
            .ToListAsync(ct);

        var actualDays = LeaveRequestService.CalculateActualLeaveDays(
            request.StartDate, request.EndDate, holidays, policy.Rules.AllowHalfDays);

        if (actualDays <= 0) return Results.BadRequest("No working days in selected range.");

        var balance = await db.LeaveBalances
            .Include(b => b.Entries)
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.PolicyId == request.PolicyId, ct);

        if (balance is not null && !policy.Rules.AllowNegativeBalance && balance.AvailableBalance < (decimal)actualDays)
            return Results.BadRequest($"Insufficient balance. Available: {balance.AvailableBalance}, Required: {actualDays}.");

        try
        {
            var leave = LeaveRequest.Create(employeeId.Value, request.PolicyId, request.StartDate,
                request.EndDate, actualDays, request.Reason);

            db.Set<LeaveRequest>().Add(leave);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/leaves/{leave.Id}", new LeaveRequestResponse(leave));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> CreateHalfDayRequest(
        [FromBody] CreateHalfDayRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (_, employeeId) = user.GetTenantAndEmployee();
        if (employeeId is null) return Results.Unauthorized();

        var policy = await db.LeavePolicies.FindAsync([request.PolicyId], ct);
        if (policy is null) return Results.NotFound("Policy not found.");
        if (!policy.Rules.AllowHalfDays) return Results.BadRequest("Half-day leave not permitted under this policy.");

        var leave = LeaveRequest.CreateHalfDay(employeeId.Value, request.PolicyId,
            request.Date, request.Period, request.Reason);

        db.Set<LeaveRequest>().Add(leave);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/leaves/{leave.Id}", new LeaveRequestResponse(leave));
    }

    private static async Task<IResult> CreateHourlyRequest(
        [FromBody] CreateHourlyRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (_, employeeId) = user.GetTenantAndEmployee();
        if (employeeId is null) return Results.Unauthorized();

        var policy = await db.LeavePolicies.FindAsync([request.PolicyId], ct);
        if (policy is null) return Results.NotFound("Policy not found.");
        if (!policy.Rules.AllowHourly) return Results.BadRequest("Hourly leave not permitted under this policy.");

        try
        {
            var leave = LeaveRequest.CreateHourly(employeeId.Value, request.PolicyId,
                request.Date, request.Hours, request.Reason);

            db.Set<LeaveRequest>().Add(leave);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/leaves/{leave.Id}", new LeaveRequestResponse(leave));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> SubmitLeaveRequest(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var leave = await db.Set<LeaveRequest>().FindAsync([id], ct);
        if (leave is null) return Results.NotFound();
        if (leave.EmployeeId != employeeId) return Results.Forbid();

        try
        {
            leave.Submit();
            await db.SaveChangesAsync(ct);
            return Results.Ok(new LeaveRequestResponse(leave));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> CancelLeaveRequest(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var leave = await db.Set<LeaveRequest>().FindAsync([id], ct);
        if (leave is null) return Results.NotFound();
        if (leave.EmployeeId != employeeId) return Results.Forbid();

        try
        {
            leave.Cancel();
            await db.SaveChangesAsync(ct);
            return Results.Ok(new LeaveRequestResponse(leave));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> WithdrawLeaveRequest(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var leave = await db.Set<LeaveRequest>().FindAsync([id], ct);
        if (leave is null) return Results.NotFound();
        if (leave.EmployeeId != employeeId) return Results.Forbid();

        try
        {
            leave.Withdraw();
            await db.SaveChangesAsync(ct);
            return Results.Ok(new LeaveRequestResponse(leave));
        }
        catch (InvalidOperationException ex)
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
            .Select(l => new LeaveRequestResponse(l))
            .ToListAsync(ct);

        return Results.Ok(leaves);
    }

    private static async Task<IResult> GetLeaveRequest(
        Guid id,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var leave = await db.Set<LeaveRequest>().FindAsync([id], ct);
        return leave is null ? Results.NotFound() : Results.Ok(new LeaveRequestResponse(leave));
    }

    private static async Task<IResult> GetBalance(
        Guid policyId,
        ClaimsPrincipal user,
        LeaveBalanceService balanceService,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var balance = await balanceService.GetCurrentBalanceAsync(employeeId.Value, policyId, ct);
        return Results.Ok(new { PolicyId = policyId, AvailableBalance = balance });
    }

    private static async Task<IResult> GetBalanceHistory(
        Guid policyId,
        [FromQuery] DateTimeOffset? asOf,
        ClaimsPrincipal user,
        LeaveBalanceService balanceService,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();

        var pointInTime = asOf ?? DateTimeOffset.UtcNow;
        var balance = await balanceService.GetHistoricalBalanceAsync(employeeId.Value, policyId, pointInTime, ct);
        return Results.Ok(new { PolicyId = policyId, AsOf = pointInTime, Balance = balance });
    }
}

public record CreateLeaveRequestRequest(Guid PolicyId, DateOnly StartDate, DateOnly EndDate, string? Reason);
public record CreateHalfDayRequest(Guid PolicyId, DateOnly Date, HalfDayPeriod Period, string? Reason);
public record CreateHourlyRequest(Guid PolicyId, DateOnly Date, double Hours, string? Reason);

public record LeaveRequestResponse(
    Guid Id,
    Guid EmployeeId,
    Guid PolicyId,
    DateOnly StartDate,
    DateOnly EndDate,
    double ActualDays,
    LeaveMode Mode,
    HalfDayPeriod? HalfDayPeriod,
    double? HoursRequested,
    LeaveRequestStatus Status,
    string? Reason,
    DateTime RequestedOnUtc)
{
    public LeaveRequestResponse(LeaveRequest r) : this(
        r.Id, r.EmployeeId, r.PolicyId, r.StartDate, r.EndDate, r.ActualDays,
        r.Mode, r.HalfDayPeriod, r.HoursRequested, r.Status, r.Reason, r.RequestedOnUtc)
    { }
}
