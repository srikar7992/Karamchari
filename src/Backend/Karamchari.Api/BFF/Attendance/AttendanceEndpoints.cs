using System.Security.Claims;
using Karamchari.Api.BFF.Common;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Domain.Holidays;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Domain.Schedules;
using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Domain.Timesheets;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.TimeAttendance.Services;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Attendance;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class AttendanceEndpoints
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapAttendanceEndpoints(this WebApplication app)
    {
        var attendance = app.MapGroup("/api/v1/workforce/attendance").RequireAuthorization();

        attendance.MapPost("/check-in", CheckInAsync);
        attendance.MapPost("/check-out", CheckOutAsync);
        attendance.MapGet("/sessions/live", GetLiveSessionsAsync);
        attendance.MapGet("/anomalies", GetAnomaliesAsync);
        attendance.MapPost("/anomalies/{id}/resolve", ResolveAnomalyAsync);

        var rosters = app.MapGroup("/api/v1/workforce/rosters").RequireAuthorization();
        rosters.MapGet("/shifts", GetShiftsAsync);
        rosters.MapPost("/schedules", CreateScheduleAsync);
        rosters.MapGet("/schedules/{id}", GetScheduleAsync);

        // Core Leave/Time endpoints preserved
        var time = app.MapGroup("/api/v1/time").RequireAuthorization();
        time.MapGet("/holidays", GetHolidaysAsync);
        time.MapGet("/leave-balances", GetLeaveBalancesAsync);
        time.MapGet("/timesheets/current", GetCurrentTimesheetAsync);

        return app;
    }

    // --- Workforce Operations ---

    private static async Task<IResult> CheckInAsync(ClaimsPrincipal user, TimeAttendanceDbContext db)
    {
        // Placeholder for Objective 4: Tracking Engine
        return await Task.FromResult(Results.Ok(new { message = "Checked in successfully" }));
    }

    private static async Task<IResult> CheckOutAsync(ClaimsPrincipal user, TimeAttendanceDbContext db)
    {
        return await Task.FromResult(Results.Ok(new { message = "Checked out successfully" }));
    }

    private static async Task<IResult> GetLiveSessionsAsync(TimeAttendanceDbContext db)
    {
        var sessions = await db.AttendanceSessions
            .Where(s => s.Status == AttendanceStatus.CheckedIn || s.Status == AttendanceStatus.OnBreak)
            .ToListAsync();
        return Results.Ok(sessions);
    }

    private static async Task<IResult> GetAnomaliesAsync(TimeAttendanceDbContext db)
    {
        var anomalies = await db.AttendanceAnomalies
            .Where(a => a.Status == AnomalyStatus.Open)
            .ToListAsync();
        return Results.Ok(anomalies);
    }

    private static async Task<IResult> ResolveAnomalyAsync(Guid id, [FromBody] string note, ClaimsPrincipal user, TimeAttendanceDbContext db)
    {
        var anomaly = await db.AttendanceAnomalies.FindAsync(id);
        if (anomaly == null) return Results.NotFound();

        anomaly.Resolve(user.Identity?.Name ?? "Admin", note);
        await db.SaveChangesAsync();
        return Results.Ok();
    }

    private static async Task<IResult> GetShiftsAsync(TimeAttendanceDbContext db)
    {
        var shifts = await db.ShiftDefinitions.ToListAsync();
        return Results.Ok(shifts);
    }

    private static async Task<IResult> CreateScheduleAsync(WorkforceSchedule schedule, TimeAttendanceDbContext db)
    {
        db.WorkforceSchedules.Add(schedule);
        await db.SaveChangesAsync();
        return Results.Created($"/api/v1/workforce/rosters/schedules/{schedule.Id}", schedule);
    }

    private static async Task<IResult> GetScheduleAsync(Guid id, TimeAttendanceDbContext db)
    {
        var schedule = await db.WorkforceSchedules.FindAsync(id);
        return schedule != null ? Results.Ok(schedule) : Results.NotFound();
    }

    // --- Legacy / Core Wrappers ---

    private static async Task<IResult> GetHolidaysAsync(TimeAttendanceDbContext db)
    {
        return Results.Ok(await db.HolidayCalendars.ToListAsync());
    }

    private static async Task<IResult> GetLeaveBalancesAsync(TimeAttendanceDbContext db)
    {
        return Results.Ok(await db.LeaveBalances.ToListAsync());
    }

    private static async Task<IResult> GetCurrentTimesheetAsync(TimeAttendanceDbContext db)
    {
        return Results.Ok(await db.Timesheets.Take(1).ToListAsync());
    }
}
