// -----------------------------------------------------------------------
// <copyright file="AttendanceReportingEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Api.BFF.Common;
using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Attendance;

public static class AttendanceReportingEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/workforce/attendance/reports")
            .RequireAuthorization()
            .WithTags("Attendance Reports");

        grp.MapGet("/daily-register", GetDailyRegisterAsync)
            .WithName("Reports.Attendance.DailyRegister");

        grp.MapGet("/monthly-summary", GetMonthlySummaryAsync)
            .WithName("Reports.Attendance.MonthlySummary");

        grp.MapGet("/overtime", GetOvertimeReportAsync)
            .WithName("Reports.Attendance.Overtime");

        grp.MapGet("/bradford", GetBradfordFactorAsync)
            .WithName("Reports.Attendance.Bradford");

        grp.MapGet("/geo-violations", GetGeoViolationsAsync)
            .WithName("Reports.Attendance.GeoViolations");

        grp.MapGet("/late-arrivals", GetLateArrivalReportAsync)
            .WithName("Reports.Attendance.LateArrivals");

        grp.MapGet("/absenteeism-trends", GetAbsenteeismTrendsAsync)
            .WithName("Reports.Attendance.AbsenteeismTrends");

        return app;
    }

    // ── Daily Register ────────────────────────────────────────────────────────

    private static async Task<IResult> GetDailyRegisterAsync(
        [FromQuery] string date,
        [FromQuery] Guid? departmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        TimeAttendanceDbContext db = default!,
        HttpContext httpContext = default!)
    {
        var user = httpContext.User;
        var (tenantId, _) = user.GetTenantAndEmployee();

        if (!DateOnly.TryParse(date, out var workDate))
            return Results.BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });

        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.AttendanceRecords
            .Where(r => r.TenantId == tenantId && r.WorkDate == workDate);

        var total = await query.CountAsync();

        var records = await query
            .OrderBy(r => r.EmployeeId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.EmployeeId,
                r.WorkDate,
                Status = r.Status.ToString(),
                r.ActualStart,
                r.ActualEnd,
                WorkedHours = Math.Round(r.TotalWorkedMinutes / 60m, 2),
                OvertimeHours = Math.Round(r.OvertimeMinutes / 60m, 2),
                r.NightDifferentialApplied,
                r.IsRegularized,
                r.FinalizedAtUtc,
            })
            .ToListAsync();

        return Results.Ok(new
        {
            Date = workDate,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Data = records,
        });
    }

    // ── Monthly Summary ───────────────────────────────────────────────────────

    private static async Task<IResult> GetMonthlySummaryAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? employeeId,
        TimeAttendanceDbContext db = default!,
        HttpContext httpContext = default!)
    {
        var user = httpContext.User;
        var (tenantId, callerEmployeeId) = user.GetTenantAndEmployee();

        if (year < 2000 || year > 2100 || month < 1 || month > 12)
            return Results.BadRequest(new { error = "Invalid year or month." });

        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var targetEmployee = employeeId ?? callerEmployeeId;

        var records = await db.AttendanceRecords
            .Where(r =>
                r.TenantId == tenantId &&
                r.EmployeeId == targetEmployee &&
                r.WorkDate >= periodStart &&
                r.WorkDate <= periodEnd)
            .ToListAsync();

        var summary = new
        {
            EmployeeId = targetEmployee,
            Year = year,
            Month = month,
            PresentDays = records.Count(r => r.Status == AttendanceStatus.Present),
            LateDays = records.Count(r => r.Status == AttendanceStatus.Late),
            AbsentDays = records.Count(r => r.Status == AttendanceStatus.Absent),
            HalfDays = records.Count(r => r.Status == AttendanceStatus.HalfDay),
            OnLeaveDays = records.Count(r => r.Status == AttendanceStatus.OnLeave),
            IncompleteDays = records.Count(r => r.Status == AttendanceStatus.Incomplete),
            RegularizedDays = records.Count(r => r.IsRegularized),
            TotalWorkedHours = Math.Round(records.Sum(r => r.TotalWorkedMinutes) / 60m, 2),
            TotalOvertimeHours = Math.Round(records.Sum(r => r.OvertimeMinutes) / 60m, 2),
            NightShiftDays = records.Count(r => r.NightDifferentialApplied),
            FinalizedRecords = records.Count(r => r.FinalizedAtUtc.HasValue),
            TotalRecords = records.Count,
        };

        return Results.Ok(summary);
    }

    // ── Overtime Report ───────────────────────────────────────────────────────

    private static async Task<IResult> GetOvertimeReportAsync(
        [FromQuery] string fromDate,
        [FromQuery] string toDate,
        [FromQuery] int minOvertimeMinutes = 30,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        TimeAttendanceDbContext db = default!,
        HttpContext httpContext = default!)
    {
        var user = httpContext.User;
        var (tenantId, _) = user.GetTenantAndEmployee();

        if (!DateOnly.TryParse(fromDate, out var from) || !DateOnly.TryParse(toDate, out var to))
            return Results.BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });

        if (to < from)
            return Results.BadRequest(new { error = "toDate must be >= fromDate." });

        pageSize = Math.Clamp(pageSize, 1, 500);

        var query = db.AttendanceRecords
            .Where(r =>
                r.TenantId == tenantId &&
                r.WorkDate >= from &&
                r.WorkDate <= to &&
                r.OvertimeMinutes >= minOvertimeMinutes);

        var total = await query.CountAsync();

        var rows = await query
            .OrderByDescending(r => r.OvertimeMinutes)
            .ThenBy(r => r.WorkDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.EmployeeId,
                r.WorkDate,
                OvertimeHours = Math.Round(r.OvertimeMinutes / 60m, 2),
                OvertimeMinutes = r.OvertimeMinutes,
                WorkedHours = Math.Round(r.TotalWorkedMinutes / 60m, 2),
                r.NightDifferentialApplied,
                NightDifferentialMultiplier = r.NightDifferentialApplied ? r.NightDifferentialMultiplier : 1.0m,
            })
            .ToListAsync();

        return Results.Ok(new
        {
            From = from,
            To = to,
            MinOvertimeMinutes = minOvertimeMinutes,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Data = rows,
        });
    }

    // ── Bradford Factor ───────────────────────────────────────────────────────

    private static async Task<IResult> GetBradfordFactorAsync(
        [FromQuery] Guid? employeeId,
        [FromQuery] string? riskLevel,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        TimeAttendanceDbContext db = default!,
        HttpContext httpContext = default!)
    {
        var user = httpContext.User;
        var (tenantId, _) = user.GetTenantAndEmployee();

        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.BradfordFactorScores
            .Where(b => b.TenantId == tenantId);

        if (employeeId.HasValue)
            query = query.Where(b => b.EmployeeId == employeeId.Value);

        if (!string.IsNullOrWhiteSpace(riskLevel) &&
            Enum.TryParse<BradfordRiskLevel>(riskLevel, ignoreCase: true, out var parsedLevel))
            query = query.Where(b => b.RiskLevel == parsedLevel);

        var total = await query.CountAsync();

        var rows = await query
            .OrderByDescending(b => b.Score)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                b.EmployeeId,
                b.Score,
                RiskLevel = b.RiskLevel.ToString(),
                b.AbsenceEpisodes,
                b.TotalAbsenceDays,
                b.RollingWindowDays,
                b.WindowStartDate,
                b.WindowEndDate,
                b.LastUpdatedAtUtc,
            })
            .ToListAsync();

        return Results.Ok(new
        {
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Data = rows,
        });
    }

    // ── Geo Violations ────────────────────────────────────────────────────────

    private static async Task<IResult> GetGeoViolationsAsync(
        [FromQuery] string fromDate,
        [FromQuery] string toDate,
        [FromQuery] Guid? employeeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        TimeAttendanceDbContext db = default!,
        HttpContext httpContext = default!)
    {
        var user = httpContext.User;
        var (tenantId, _) = user.GetTenantAndEmployee();

        if (!DateOnly.TryParse(fromDate, out var from) || !DateOnly.TryParse(toDate, out var to))
            return Results.BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });

        pageSize = Math.Clamp(pageSize, 1, 500);

        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var query = db.AttendanceViolations
            .Where(v =>
                v.TenantId == tenantId &&
                v.ExceptionType == AttendanceExceptionType.LocationViolation &&
                v.CreatedAt >= fromDt &&
                v.CreatedAt <= toDt);

        if (employeeId.HasValue)
            query = query.Where(v => v.EmployeeId == employeeId.Value);

        var total = await query.CountAsync();

        var rows = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new
            {
                v.Id,
                v.EmployeeId,
                v.AttendanceRecordId,
                Severity = v.Severity.ToString(),
                v.Description,
                RaisedAt = v.CreatedAt,
                Status = v.Status.ToString(),
                v.ResolvedAt,
                ResolutionNote = v.Resolution,
            })
            .ToListAsync();

        return Results.Ok(new
        {
            From = from,
            To = to,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Data = rows,
        });
    }

    // ── Late Arrival Report ───────────────────────────────────────────────────

    private static async Task<IResult> GetLateArrivalReportAsync(
        [FromQuery] string fromDate,
        [FromQuery] string toDate,
        [FromQuery] Guid? employeeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        TimeAttendanceDbContext db = default!,
        HttpContext httpContext = default!)
    {
        var user = httpContext.User;
        var (tenantId, _) = user.GetTenantAndEmployee();

        if (!DateOnly.TryParse(fromDate, out var from) || !DateOnly.TryParse(toDate, out var to))
            return Results.BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });

        pageSize = Math.Clamp(pageSize, 1, 500);

        var query = db.AttendanceRecords
            .Where(r =>
                r.TenantId == tenantId &&
                r.WorkDate >= from &&
                r.WorkDate <= to &&
                r.Status == AttendanceStatus.Late);

        if (employeeId.HasValue)
            query = query.Where(r => r.EmployeeId == employeeId.Value);

        var total = await query.CountAsync();

        var rows = await query
            .OrderBy(r => r.EmployeeId)
            .ThenBy(r => r.WorkDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.EmployeeId,
                r.WorkDate,
                r.ExpectedStart,
                r.ActualStart,
                LateByMinutes = r.ActualStart.HasValue
                    ? (int)(r.ActualStart.Value - r.ExpectedStart).TotalMinutes
                    : (int?)null,
                r.IsRegularized,
            })
            .ToListAsync();

        // Per-employee habitual lateness summary
        var byEmployee = rows
            .GroupBy(r => r.EmployeeId)
            .Select(g => new
            {
                EmployeeId = g.Key,
                LateCount = g.Count(),
                AvgLateMinutes = g.Where(r => r.LateByMinutes.HasValue)
                    .Select(r => r.LateByMinutes!.Value)
                    .DefaultIfEmpty(0)
                    .Average(),
            })
            .OrderByDescending(x => x.LateCount)
            .ToList();

        return Results.Ok(new
        {
            From = from,
            To = to,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            EmployeeSummary = byEmployee,
            Data = rows,
        });
    }

    // ── Absenteeism Trends ────────────────────────────────────────────────────

    private static async Task<IResult> GetAbsenteeismTrendsAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? employeeId,
        TimeAttendanceDbContext db = default!,
        HttpContext httpContext = default!)
    {
        var user = httpContext.User;
        var (tenantId, _) = user.GetTenantAndEmployee();

        if (year < 2000 || year > 2100 || month < 1 || month > 12)
            return Results.BadRequest(new { error = "Invalid year or month." });

        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var query = db.AttendanceRecords
            .Where(r =>
                r.TenantId == tenantId &&
                r.WorkDate >= periodStart &&
                r.WorkDate <= periodEnd &&
                r.Status == AttendanceStatus.Absent);

        if (employeeId.HasValue)
            query = query.Where(r => r.EmployeeId == employeeId.Value);

        var absentRecords = await query
            .Select(r => new { r.EmployeeId, r.WorkDate })
            .ToListAsync();

        // Day-of-week breakdown
        var byDayOfWeek = absentRecords
            .GroupBy(r => r.WorkDate.DayOfWeek)
            .Select(g => new { DayOfWeek = g.Key.ToString(), AbsenceCount = g.Count() })
            .OrderBy(x => (int)Enum.Parse<DayOfWeek>(x.DayOfWeek))
            .ToList();

        // Weekly trend within the month
        var byWeek = absentRecords
            .GroupBy(r => (r.WorkDate.DayNumber - periodStart.DayNumber) / 7 + 1)
            .Select(g => new { Week = g.Key, AbsenceCount = g.Count() })
            .OrderBy(x => x.Week)
            .ToList();

        // Top absentees
        var topAbsentees = absentRecords
            .GroupBy(r => r.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, AbsenceDays = g.Count() })
            .OrderByDescending(x => x.AbsenceDays)
            .Take(20)
            .ToList();

        return Results.Ok(new
        {
            Year = year,
            Month = month,
            TotalAbsences = absentRecords.Count,
            ByDayOfWeek = byDayOfWeek,
            ByWeek = byWeek,
            TopAbsentees = topAbsentees,
        });
    }
}
