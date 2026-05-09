using System.Security.Claims;
using Karamchari.Api.BFF.Common;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.Holidays;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Domain.Timesheets;
using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Domain.Schedules;
using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.TimeAttendance.Services;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Attendance;

public static class AttendanceEndpoints
{
    public static WebApplication MapAttendanceEndpoints(this WebApplication app)
    {
        var attendance = app.MapGroup("/api/v1/workforce/attendance").RequireAuthorization();
        
        attendance.MapPost("/check-in", CheckIn);
        attendance.MapPost("/check-out", CheckOut);
        attendance.MapGet("/sessions/live", GetLiveSessions);
        attendance.MapGet("/anomalies", GetAnomalies);
        attendance.MapPost("/anomalies/{id}/resolve", ResolveAnomaly);

        var rosters = app.MapGroup("/api/v1/workforce/rosters").RequireAuthorization();
        rosters.MapGet("/shifts", GetShifts);
        rosters.MapPost("/schedules", CreateSchedule);
        rosters.MapGet("/schedules/{id}", GetSchedule);

        // Core Leave/Time endpoints preserved
        var time = app.MapGroup("/api/v1/time").RequireAuthorization();
        time.MapGet("/holidays", GetHolidays);
        time.MapGet("/leave-balances", GetLeaveBalances);
        time.MapGet("/timesheets/current", GetCurrentTimesheet);

        return app;
    }

    // --- Workforce Operations ---

    private static async Task<IResult> CheckIn(ClaimsPrincipal user, TimeAttendanceDbContext db)
    {
        // Placeholder for Objective 4: Tracking Engine
        return Results.Ok(new { message = "Checked in successfully" });
    }

    private static async Task<IResult> CheckOut(ClaimsPrincipal user, TimeAttendanceDbContext db)
    {
        return Results.Ok(new { message = "Checked out successfully" });
    }

    private static async Task<IResult> GetLiveSessions(TimeAttendanceDbContext db)
    {
        var sessions = await db.AttendanceSessions
            .Where(s => s.Status == AttendanceStatus.CheckedIn || s.Status == AttendanceStatus.OnBreak)
            .ToListAsync();
        return Results.Ok(sessions);
    }

    private static async Task<IResult> GetAnomalies(TimeAttendanceDbContext db)
    {
        var anomalies = await db.AttendanceAnomalies
            .Where(a => a.Status == AnomalyStatus.Open)
            .ToListAsync();
        return Results.Ok(anomalies);
    }

    private static async Task<IResult> ResolveAnomaly(Guid id, [FromBody] string note, ClaimsPrincipal user, TimeAttendanceDbContext db)
    {
        var anomaly = await db.AttendanceAnomalies.FindAsync(id);
        if (anomaly == null) return Results.NotFound();

        anomaly.Resolve(user.Identity?.Name ?? "Admin", note);
        await db.SaveChangesAsync();
        return Results.Ok();
    }

    private static async Task<IResult> GetShifts(TimeAttendanceDbContext db)
    {
        var shifts = await db.ShiftDefinitions.ToListAsync();
        return Results.Ok(shifts);
    }

    private static async Task<IResult> CreateSchedule(WorkforceSchedule schedule, TimeAttendanceDbContext db)
    {
        db.WorkforceSchedules.Add(schedule);
        await db.SaveChangesAsync();
        return Results.Created($"/api/v1/workforce/rosters/schedules/{schedule.Id}", schedule);
    }

    private static async Task<IResult> GetSchedule(Guid id, TimeAttendanceDbContext db)
    {
        var schedule = await db.WorkforceSchedules.FindAsync(id);
        return schedule != null ? Results.Ok(schedule) : Results.NotFound();
    }

    // --- Legacy / Core Wrappers ---

    private static async Task<IResult> GetHolidays(TimeAttendanceDbContext db)
    {
        return Results.Ok(await db.HolidayCalendars.ToListAsync());
    }

    private static async Task<IResult> GetLeaveBalances(TimeAttendanceDbContext db)
    {
        return Results.Ok(await db.LeaveBalances.ToListAsync());
    }

    private static async Task<IResult> GetCurrentTimesheet(TimeAttendanceDbContext db)
    {
        return Results.Ok(await db.Timesheets.Take(1).ToListAsync());
    }
}
