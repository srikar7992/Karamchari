using System.Security.Claims;
using Karamchari.Api.BFF.Common;
using Karamchari.Api.Middleware;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Contracts;
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

public static class AttendanceEndpoints
{
    public static WebApplication MapAttendanceEndpoints(this WebApplication app)
    {
        var attendance = app.MapGroup("/api/v1/workforce/attendance").RequireAuthorization();

        // Punch endpoints
        attendance.MapPost("/check-in", CheckInAsync).WithName("Attendance.CheckIn").WithIdempotency();
        attendance.MapPost("/check-out", CheckOutAsync).WithName("Attendance.CheckOut");
        attendance.MapPost("/break/start", StartBreakAsync).WithName("Attendance.Break.Start");
        attendance.MapPost("/break/end", EndBreakAsync).WithName("Attendance.Break.End");

        // Live operations
        attendance.MapGet("/sessions/live", GetLiveSessionsAsync).WithName("Attendance.Sessions.Live");
        attendance.MapGet("/anomalies", GetAnomaliesAsync).WithName("Attendance.Anomalies");
        attendance.MapPost("/anomalies/{id:guid}/resolve", ResolveAnomalyAsync).WithName("Attendance.Anomaly.Resolve");

        // Records
        attendance.MapGet("/records", GetRecordsAsync).WithName("Attendance.Records");
        attendance.MapGet("/records/{id:guid}", GetRecordAsync).WithName("Attendance.Record.Get");
        attendance.MapGet("/records/my", GetMyRecordsAsync).WithName("Attendance.Records.My");

        // Exceptions
        attendance.MapGet("/exceptions", GetExceptionsAsync).WithName("Attendance.Exceptions");
        attendance.MapPost("/exceptions/{id:guid}/acknowledge", AcknowledgeExceptionAsync).WithName("Attendance.Exception.Acknowledge");
        attendance.MapPost("/exceptions/{id:guid}/waive", WaiveExceptionAsync).WithName("Attendance.Exception.Waive");

        // Regularization
        attendance.MapPost("/regularizations", SubmitRegularizationAsync).WithName("Attendance.Regularization.Submit").WithIdempotency();
        attendance.MapGet("/regularizations", GetRegularizationsAsync).WithName("Attendance.Regularizations");
        attendance.MapGet("/regularizations/pending", GetPendingRegularizationsAsync).WithName("Attendance.Regularizations.Pending");
        attendance.MapPost("/regularizations/{id:guid}/approve", ApproveRegularizationAsync).WithName("Attendance.Regularization.Approve");
        attendance.MapPost("/regularizations/{id:guid}/reject", RejectRegularizationAsync).WithName("Attendance.Regularization.Reject");
        attendance.MapPost("/regularizations/{id:guid}/cancel", CancelRegularizationAsync).WithName("Attendance.Regularization.Cancel");

        // Scores / Intelligence
        attendance.MapGet("/scores/me", GetMyScoreAsync).WithName("Attendance.Score.Me");
        attendance.MapGet("/scores/{employeeId:guid}", GetEmployeeScoreAsync).WithName("Attendance.Score.Employee");
        attendance.MapGet("/scores", GetTeamScoresAsync).WithName("Attendance.Score.Team");

        // Dashboards
        attendance.MapGet("/dashboard/health", GetAttendanceHealthAsync).WithName("Attendance.Dashboard.Health");
        attendance.MapGet("/dashboard/manager", GetManagerDashboardAsync).WithName("Attendance.Dashboard.Manager");

        // Policies
        attendance.MapGet("/policies", GetPoliciesAsync).WithName("Attendance.Policy.List");
        attendance.MapPost("/policies", CreatePolicyAsync).WithName("Attendance.Policy.Create").WithIdempotency();
        attendance.MapPut("/policies/{id:guid}", UpdatePolicyAsync).WithName("Attendance.Policy.Update");

        // Legacy wrappers preserved
        var time = app.MapGroup("/api/v1/time").RequireAuthorization();
        time.MapGet("/holidays", GetHolidaysAsync);
        time.MapGet("/leave-balances", GetLeaveBalancesAsync);
        time.MapGet("/timesheets/current", GetCurrentTimesheetAsync);

        var legacyRosters = app.MapGroup("/api/v1/workforce/rosters").RequireAuthorization();
        legacyRosters.MapGet("/shifts", GetShiftsAsync);
        legacyRosters.MapPost("/schedules", CreateScheduleAsync);
        legacyRosters.MapGet("/schedules/{id:guid}", GetScheduleAsync);

        return app;
    }

    // ── Punch Operations ──────────────────────────────────────────────────────────

    private static async Task<IResult> CheckInAsync(
        [FromBody] CheckInRequest request,
        ClaimsPrincipal user,
        AttendanceProcessingEngine engine,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        GeoPoint? location = null;
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            try { location = GeoPoint.Create(request.Latitude.Value, request.Longitude.Value); }
            catch (ArgumentOutOfRangeException) { return Results.BadRequest("Invalid GPS coordinates."); }
        }

        var source = Enum.TryParse<AttendanceSource>(request.Source, true, out var s) ? s : AttendanceSource.MobileApp;

        try
        {
            var session = await engine.ProcessCheckInAsync(
                tenantId, employeeId.Value,
                DateTimeOffset.UtcNow,
                location,
                request.DeviceId,
                source,
                ct);

            return Results.Ok(new { sessionId = session.Id, checkInTime = session.CheckInTime, status = session.Status.ToString() });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> CheckOutAsync(
        [FromBody] CheckOutRequest request,
        ClaimsPrincipal user,
        AttendanceProcessingEngine engine,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        GeoPoint? location = null;
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            try { location = GeoPoint.Create(request.Latitude.Value, request.Longitude.Value); }
            catch (ArgumentOutOfRangeException) { return Results.BadRequest("Invalid GPS coordinates."); }
        }

        var record = await engine.ProcessCheckOutAsync(
            tenantId, employeeId.Value,
            DateTimeOffset.UtcNow,
            location,
            ct: ct);

        if (record is null)
            return Results.BadRequest("No active check-in session found. Cannot check out.");

        return Results.Ok(new
        {
            recordId = record.Id,
            status = record.Status.ToString(),
            totalWorkedMinutes = record.TotalWorkedMinutes,
            overtimeMinutes = record.OvertimeMinutes
        });
    }

    private static async Task<IResult> StartBreakAsync(
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (_, employeeId) = user.GetTenantAndEmployee();
        if (employeeId is null) return Results.Unauthorized();

        var workDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var session = await db.AttendanceSessions
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s =>
                s.EmployeeId == employeeId &&
                s.WorkDate == workDate &&
                s.Status == AttendanceStatus.CheckedIn, ct);

        if (session is null) return Results.BadRequest("No active check-in session.");

        try
        {
            session.StartBreak(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Break started." });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> EndBreakAsync(
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (_, employeeId) = user.GetTenantAndEmployee();
        if (employeeId is null) return Results.Unauthorized();

        var workDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var session = await db.AttendanceSessions
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s =>
                s.EmployeeId == employeeId &&
                s.WorkDate == workDate &&
                s.Status == AttendanceStatus.OnBreak, ct);

        if (session is null) return Results.BadRequest("No active break session.");

        try
        {
            session.EndBreak(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Break ended." });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    // ── Live Operations ───────────────────────────────────────────────────────────

    private static async Task<IResult> GetLiveSessionsAsync(
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var sessions = await db.AttendanceSessions
            .Where(s => s.TenantId == tenantId &&
                       (s.Status == AttendanceStatus.CheckedIn || s.Status == AttendanceStatus.OnBreak))
            .Select(s => new
            {
                s.Id,
                s.EmployeeId,
                s.WorkDate,
                s.Status,
                s.CheckInTime,
                s.TotalWorkHours
            })
            .ToListAsync(ct);

        return Results.Ok(sessions);
    }

    private static async Task<IResult> GetAnomaliesAsync(
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var anomalies = await db.AttendanceAnomalies
            .Where(a => a.TenantId == tenantId && a.Status == AnomalyStatus.Open)
            .ToListAsync(ct);

        return Results.Ok(anomalies);
    }

    private static async Task<IResult> ResolveAnomalyAsync(
        Guid id,
        [FromBody] string note,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var anomaly = await db.AttendanceAnomalies.FindAsync([id], ct);
        if (anomaly is null) return Results.NotFound();

        anomaly.Resolve(user.Identity?.Name ?? "system", note);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ── Records ───────────────────────────────────────────────────────────────────

    private static async Task<IResult> GetRecordsAsync(
        [FromQuery] Guid? employeeId,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] string? status,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var start = string.IsNullOrEmpty(startDate)
            ? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30))
            : DateOnly.Parse(startDate);
        var end = string.IsNullOrEmpty(endDate)
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : DateOnly.Parse(endDate);

        var query = db.AttendanceRecords
            .Where(r => r.TenantId == tenantId && r.WorkDate >= start && r.WorkDate <= end);

        if (employeeId.HasValue)
            query = query.Where(r => r.EmployeeId == employeeId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AttendanceStatus>(status, true, out var s))
            query = query.Where(r => r.Status == s);

        var records = await query
            .OrderByDescending(r => r.WorkDate)
            .Select(r => new AttendanceRecordResponse(r))
            .ToListAsync(ct);

        return Results.Ok(records);
    }

    private static async Task<IResult> GetRecordAsync(Guid id, TimeAttendanceDbContext db, CancellationToken ct)
    {
        var record = await db.AttendanceRecords.FindAsync([id], ct);
        return record is null ? Results.NotFound() : Results.Ok(new AttendanceRecordResponse(record));
    }

    private static async Task<IResult> GetMyRecordsAsync(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var start = string.IsNullOrEmpty(startDate)
            ? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30))
            : DateOnly.Parse(startDate);
        var end = string.IsNullOrEmpty(endDate)
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : DateOnly.Parse(endDate);

        var records = await db.AttendanceRecords
            .Where(r => r.EmployeeId == employeeId && r.WorkDate >= start && r.WorkDate <= end)
            .OrderByDescending(r => r.WorkDate)
            .Select(r => new AttendanceRecordResponse(r))
            .ToListAsync(ct);

        return Results.Ok(records);
    }

    // ── Exceptions ────────────────────────────────────────────────────────────────

    private static async Task<IResult> GetExceptionsAsync(
        [FromQuery] Guid? employeeId,
        [FromQuery] string? severity,
        [FromQuery] string? status,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var query = db.AttendanceViolations.Where(e => e.TenantId == tenantId);

        if (employeeId.HasValue)
            query = query.Where(e => e.EmployeeId == employeeId.Value);

        if (!string.IsNullOrEmpty(severity) && Enum.TryParse<AttendanceExceptionSeverity>(severity, true, out var sev))
            query = query.Where(e => e.Severity == sev);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AttendanceExceptionStatus>(status, true, out var st))
            query = query.Where(e => e.Status == st);
        else
            query = query.Where(e => e.Status == AttendanceExceptionStatus.Open);

        var exceptions = await query
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new
            {
                e.Id,
                e.AttendanceRecordId,
                e.EmployeeId,
                ExceptionType = e.ExceptionType.ToString(),
                Severity = e.Severity.ToString(),
                e.Description,
                Status = e.Status.ToString(),
                e.CreatedAt
            })
            .ToListAsync(ct);

        return Results.Ok(exceptions);
    }

    private static async Task<IResult> AcknowledgeExceptionAsync(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var ex = await db.AttendanceViolations.FindAsync([id], ct);
        if (ex is null) return Results.NotFound();

        try
        {
            ex.Acknowledge(user.Identity?.Name ?? "system");
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        }
        catch (InvalidOperationException e)
        {
            return Results.Conflict(new { error = e.Message });
        }
    }

    private static async Task<IResult> WaiveExceptionAsync(
        Guid id,
        [FromBody] WaiveExceptionRequest request,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Results.BadRequest("Waiver reason is required.");

        var ex = await db.AttendanceViolations.FindAsync([id], ct);
        if (ex is null) return Results.NotFound();

        try
        {
            ex.Waive(request.Reason);
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        }
        catch (InvalidOperationException e)
        {
            return Results.Conflict(new { error = e.Message });
        }
    }

    // ── Regularization ────────────────────────────────────────────────────────────

    private static async Task<IResult> SubmitRegularizationAsync(
        [FromBody] SubmitRegularizationRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var record = await db.AttendanceRecords.FindAsync([request.AttendanceRecordId], ct);
        if (record is null) return Results.NotFound("Attendance record not found.");

        // Only own records unless manager (simplified — full RBAC in real implementation)
        if (record.EmployeeId != employeeId.Value)
            return Results.Forbid();

        // Check no open regularization already exists for this record
        var existingPending = await db.RegularizationRequests
            .AnyAsync(r =>
                r.AttendanceRecordId == request.AttendanceRecordId &&
                r.Status == RegularizationStatus.Submitted,
                ct);
        if (existingPending)
            return Results.Conflict(new { error = "A pending regularization already exists for this attendance record." });

        try
        {
            var regularization = RegularizationRequest.Submit(
                tenantId,
                record.Id,
                employeeId.Value,
                record.Status,
                request.Explanation,
                request.Evidence);

            db.RegularizationRequests.Add(regularization);
            await db.SaveChangesAsync(ct);

            return Results.Created(
                $"/api/v1/workforce/attendance/regularizations/{regularization.Id}",
                new { id = regularization.Id, status = regularization.Status.ToString() });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetRegularizationsAsync(
        [FromQuery] Guid? employeeId,
        [FromQuery] string? status,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var query = db.RegularizationRequests.Where(r => r.TenantId == tenantId);

        if (employeeId.HasValue)
            query = query.Where(r => r.EmployeeId == employeeId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<RegularizationStatus>(status, true, out var s))
            query = query.Where(r => r.Status == s);

        var results = await query
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => new
            {
                r.Id,
                r.EmployeeId,
                r.AttendanceRecordId,
                r.Explanation,
                r.Evidence,
                Status = r.Status.ToString(),
                r.RequestedAt,
                r.ApprovedAt,
                r.ApprovedBy,
                r.RejectionReason
            })
            .ToListAsync(ct);

        return Results.Ok(results);
    }

    private static async Task<IResult> GetPendingRegularizationsAsync(
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var results = await db.RegularizationRequests
            .Where(r => r.TenantId == tenantId && r.Status == RegularizationStatus.Submitted)
            .OrderBy(r => r.RequestedAt)
            .Select(r => new { r.Id, r.EmployeeId, r.AttendanceRecordId, r.Explanation, r.RequestedAt })
            .ToListAsync(ct);

        return Results.Ok(results);
    }

    private static async Task<IResult> ApproveRegularizationAsync(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        AttendanceProcessingEngine engine,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var (tenantId, approverId) = user.GetTenantAndEmployee();
        if (tenantId is null || approverId is null) return Results.Unauthorized();

        var request = await db.RegularizationRequests.FindAsync([id], ct);
        if (request is null) return Results.NotFound();

        try
        {
            request.Approve(approverId.Value);
            await db.SaveChangesAsync(ct);

            await bus.Publish(new RegularizationApprovedIntegrationEvent(
                Guid.NewGuid(),
                tenantId,
                request.EmployeeId,
                request.Id,
                approverId.Value,
                DateTimeOffset.UtcNow), ct);

            // Apply the correction to the attendance record
            await engine.ApplyRegularizationAsync(tenantId, request.AttendanceRecordId, request.EmployeeId, approverId.Value, ct);

            return Results.Ok(new { status = request.Status.ToString() });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> RejectRegularizationAsync(
        Guid id,
        [FromBody] RejectRegularizationRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Results.BadRequest("Rejection reason is required.");

        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var regularization = await db.RegularizationRequests.FindAsync([id], ct);
        if (regularization is null) return Results.NotFound();

        try
        {
            regularization.Reject(request.Reason);
            await db.SaveChangesAsync(ct);

            await bus.Publish(new RegularizationRejectedIntegrationEvent(
                Guid.NewGuid(),
                tenantId,
                regularization.EmployeeId,
                regularization.Id,
                request.Reason,
                DateTimeOffset.UtcNow), ct);

            return Results.Ok(new { status = regularization.Status.ToString() });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> CancelRegularizationAsync(
        Guid id,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (_, employeeId) = user.GetTenantAndEmployee();
        if (employeeId is null) return Results.Unauthorized();

        var request = await db.RegularizationRequests.FindAsync([id], ct);
        if (request is null) return Results.NotFound();
        if (request.EmployeeId != employeeId.Value) return Results.Forbid();

        try
        {
            request.Cancel();
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    // ── Scores / Intelligence ─────────────────────────────────────────────────────

    private static async Task<IResult> GetMyScoreAsync(
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (_, employeeId) = user.GetTenantAndEmployee();
        if (employeeId is null) return Results.Unauthorized();

        var score = await db.AttendanceScores
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId, ct);

        return score is null
            ? Results.Ok(new { reliabilityScore = 100m, consistencyScore = 100m, complianceScore = 100m, message = "No attendance data yet." })
            : Results.Ok(new AttendanceScoreResponse(score));
    }

    private static async Task<IResult> GetEmployeeScoreAsync(
        Guid employeeId,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var score = await db.AttendanceScores
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId, ct);

        return score is null ? Results.NotFound() : Results.Ok(new AttendanceScoreResponse(score));
    }

    private static async Task<IResult> GetTeamScoresAsync(
        [FromQuery] string? sort,
        [FromQuery] int take = 20,
        ClaimsPrincipal user = null!,
        TimeAttendanceDbContext db = null!,
        CancellationToken ct = default)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var query = db.AttendanceScores.Where(s => s.TenantId == tenantId);

        query = sort switch
        {
            "reliability_asc" => query.OrderBy(s => s.ReliabilityScore),
            "reliability_desc" => query.OrderByDescending(s => s.ReliabilityScore),
            "consistency_asc" => query.OrderBy(s => s.ConsistencyScore),
            "risk" => query.OrderBy(s => s.ReliabilityScore), // lowest reliability = highest risk
            _ => query.OrderByDescending(s => s.ReliabilityScore)
        };

        var scores = await query
            .Take(Math.Min(take, 100))
            .Select(s => new AttendanceScoreResponse(s))
            .ToListAsync(ct);

        return Results.Ok(scores);
    }

    // ── Dashboards ────────────────────────────────────────────────────────────────

    private static async Task<IResult> GetAttendanceHealthAsync(
        [FromQuery] string? date,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var targetDate = string.IsNullOrEmpty(date)
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : DateOnly.Parse(date);

        var records = await db.AttendanceRecords
            .Where(r => r.TenantId == tenantId && r.WorkDate == targetDate)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(ct);

        var openExceptions = await db.AttendanceViolations
            .Where(e => e.TenantId == tenantId &&
                       e.Status == AttendanceExceptionStatus.Open &&
                       e.CreatedAt.Date == targetDate.ToDateTime(TimeOnly.MinValue).Date)
            .GroupBy(e => e.ExceptionType)
            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(ct);

        var pendingRegularizations = await db.RegularizationRequests
            .CountAsync(r => r.TenantId == tenantId && r.Status == RegularizationStatus.Submitted, ct);

        return Results.Ok(new
        {
            Date = targetDate,
            AttendanceSummary = records,
            OpenExceptions = openExceptions,
            PendingRegularizations = pendingRegularizations
        });
    }

    private static async Task<IResult> GetManagerDashboardAsync(
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Employees requiring action: open Critical/High exceptions unacknowledged
        var requiresAction = await db.AttendanceViolations
            .Where(e => e.TenantId == tenantId &&
                       e.Status == AttendanceExceptionStatus.Open &&
                       (e.Severity == AttendanceExceptionSeverity.Critical ||
                        e.Severity == AttendanceExceptionSeverity.High))
            .Select(e => e.EmployeeId)
            .Distinct()
            .CountAsync(ct);

        var pendingRegularizations = await db.RegularizationRequests
            .CountAsync(r => r.TenantId == tenantId && r.Status == RegularizationStatus.Submitted, ct);

        // High-risk employees: reliability score < 80
        var highRisk = await db.AttendanceScores
            .Where(s => s.TenantId == tenantId && s.ReliabilityScore < 80)
            .CountAsync(ct);

        // Compliance percentage: employees with compliance score >= 90
        var allScores = await db.AttendanceScores
            .Where(s => s.TenantId == tenantId)
            .Select(s => new { s.ComplianceScore })
            .ToListAsync(ct);

        var compliancePercent = allScores.Count == 0 ? 100m
            : Math.Round(allScores.Count(s => s.ComplianceScore >= 90) * 100m / allScores.Count, 1);

        // Today's no-shows
        var noShowsToday = await db.AttendanceRecords
            .CountAsync(r => r.TenantId == tenantId &&
                            r.WorkDate == today &&
                            r.Status == AttendanceStatus.Absent, ct);

        return Results.Ok(new
        {
            EmployeesRequiringAction = requiresAction,
            PendingRegularizations = pendingRegularizations,
            HighRiskEmployees = highRisk,
            AttendanceCompliancePercent = compliancePercent,
            NoShowsToday = noShowsToday
        });
    }

    // ── Policies ──────────────────────────────────────────────────────────────────

    private static async Task<IResult> GetPoliciesAsync(
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var policies = await db.AttendanceShiftPolicies
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.IsDefault ? 0 : 1)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);

        return Results.Ok(policies);
    }

    private static async Task<IResult> CreatePolicyAsync(
        [FromBody] CreateAttendancePolicyRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        try
        {
            var policy = request.LocationId.HasValue
                ? AttendanceShiftPolicy.Create(
                    tenantId,
                    request.Name,
                    request.LocationId.Value,
                    request.LateToleranceMinutes,
                    request.EarlyExitToleranceMinutes,
                    request.MaxMissingPunchesPerMonth,
                    request.OvertimeThresholdMinutes,
                    request.EnforceGeofencing,
                    request.GeofenceRadiusMeters)
                : AttendanceShiftPolicy.CreateDefault(tenantId);

            db.AttendanceShiftPolicies.Add(policy);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/workforce/attendance/policies/{policy.Id}", policy);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpdatePolicyAsync(
        Guid id,
        [FromBody] UpdateAttendancePolicyRequest request,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var policy = await db.AttendanceShiftPolicies.FindAsync([id], ct);
        if (policy is null) return Results.NotFound();

        policy.Update(
            request.LateToleranceMinutes,
            request.EarlyExitToleranceMinutes,
            request.MaxMissingPunchesPerMonth,
            request.OvertimeThresholdMinutes,
            request.EnforceGeofencing,
            request.GeofenceRadiusMeters,
            request.GeofenceWarningRadiusMeters,
            request.GeofenceViolationRadiusMeters);

        await db.SaveChangesAsync(ct);
        return Results.Ok(policy);
    }

    // ── Legacy wrappers ───────────────────────────────────────────────────────────

    private static async Task<IResult> GetHolidaysAsync(TimeAttendanceDbContext db, CancellationToken ct)
        => Results.Ok(await db.HolidayCalendars.ToListAsync(ct));

    private static async Task<IResult> GetLeaveBalancesAsync(TimeAttendanceDbContext db, CancellationToken ct)
        => Results.Ok(await db.LeaveBalances.ToListAsync(ct));

    private static async Task<IResult> GetCurrentTimesheetAsync(TimeAttendanceDbContext db, CancellationToken ct)
        => Results.Ok(await db.Timesheets.Take(1).ToListAsync(ct));

    private static async Task<IResult> GetShiftsAsync(TimeAttendanceDbContext db, CancellationToken ct)
        => Results.Ok(await db.ShiftDefinitions.ToListAsync(ct));

    private static async Task<IResult> CreateScheduleAsync(
        [FromBody] WorkforceSchedule schedule,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        db.WorkforceSchedules.Add(schedule);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/workforce/rosters/schedules/{schedule.Id}", schedule);
    }

    private static async Task<IResult> GetScheduleAsync(
        Guid id,
        TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var schedule = await db.WorkforceSchedules.FindAsync([id], ct);
        return schedule is not null ? Results.Ok(schedule) : Results.NotFound();
    }
}

// ── Request/Response types ─────────────────────────────────────────────────────

public sealed record CheckInRequest(
    double? Latitude,
    double? Longitude,
    string? DeviceId,
    string Source = "MobileApp");

public sealed record CheckOutRequest(
    double? Latitude,
    double? Longitude);

public sealed record WaiveExceptionRequest(string Reason);

public sealed record SubmitRegularizationRequest(
    Guid AttendanceRecordId,
    string Explanation,
    string? Evidence = null);

public sealed record RejectRegularizationRequest(string Reason);

public sealed record CreateAttendancePolicyRequest(
    string Name,
    Guid? LocationId = null,
    int LateToleranceMinutes = 5,
    int EarlyExitToleranceMinutes = 5,
    int MaxMissingPunchesPerMonth = 3,
    int OvertimeThresholdMinutes = 30,
    bool EnforceGeofencing = false,
    int GeofenceRadiusMeters = 100);

public sealed record UpdateAttendancePolicyRequest(
    int LateToleranceMinutes,
    int EarlyExitToleranceMinutes,
    int MaxMissingPunchesPerMonth,
    int OvertimeThresholdMinutes,
    bool EnforceGeofencing,
    int GeofenceRadiusMeters,
    int GeofenceWarningRadiusMeters = 200,
    int GeofenceViolationRadiusMeters = 500);

public sealed record AttendanceRecordResponse
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public Guid ShiftId { get; init; }
    public DateOnly WorkDate { get; init; }
    public DateTimeOffset ExpectedStart { get; init; }
    public DateTimeOffset ExpectedEnd { get; init; }
    public DateTimeOffset? ActualStart { get; init; }
    public DateTimeOffset? ActualEnd { get; init; }
    public string Status { get; init; }
    public int TotalWorkedMinutes { get; init; }
    public int OvertimeMinutes { get; init; }
    public bool IsRegularized { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public AttendanceRecordResponse(AttendanceRecord r)
    {
        Id = r.Id;
        EmployeeId = r.EmployeeId;
        ShiftId = r.ShiftId;
        WorkDate = r.WorkDate;
        ExpectedStart = r.ExpectedStart;
        ExpectedEnd = r.ExpectedEnd;
        ActualStart = r.ActualStart;
        ActualEnd = r.ActualEnd;
        Status = r.Status.ToString();
        TotalWorkedMinutes = r.TotalWorkedMinutes;
        OvertimeMinutes = r.OvertimeMinutes;
        IsRegularized = r.IsRegularized;
        CreatedAt = r.CreatedAt;
    }
}

public sealed record AttendanceScoreResponse
{
    public Guid EmployeeId { get; init; }
    public decimal ReliabilityScore { get; init; }
    public decimal ConsistencyScore { get; init; }
    public decimal ComplianceScore { get; init; }
    public int TotalAssignedShifts { get; init; }
    public int TotalPresentShifts { get; init; }
    public int TotalLateArrivals { get; init; }
    public int TotalNoShows { get; init; }
    public DateTimeOffset LastCalculated { get; init; }

    public AttendanceScoreResponse(AttendanceScore s)
    {
        EmployeeId = s.EmployeeId;
        ReliabilityScore = s.ReliabilityScore;
        ConsistencyScore = s.ConsistencyScore;
        ComplianceScore = s.ComplianceScore;
        TotalAssignedShifts = s.TotalAssignedShifts;
        TotalPresentShifts = s.TotalPresentShifts;
        TotalLateArrivals = s.TotalLateArrivals;
        TotalNoShows = s.TotalNoShows;
        LastCalculated = s.LastCalculated;
    }
}
