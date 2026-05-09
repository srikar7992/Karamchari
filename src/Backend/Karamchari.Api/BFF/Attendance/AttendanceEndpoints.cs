using System.Security.Claims;
using Karamchari.Api.BFF.Common;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.Holidays;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Domain.Timesheets;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.TimeAttendance.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Attendance;

public static class AttendanceEndpoints
{
    public static WebApplication MapAttendanceEndpoints(this WebApplication app)
    {
        var attendance = app.MapGroup("/api/attendance").RequireAuthorization();
        
        attendance.MapGet("/dlq", GetDlq);
        attendance.MapPost("/dlq/{id}/map", MapDlq);
        attendance.MapGet("/fraud", GetFraud);
        attendance.MapPut("/fraud/{id}/review", ReviewFraud);
        attendance.MapPost("/reprocess", Reprocess);
        attendance.MapGet("/reprocess/{jobId}", GetReprocessJob);

        var time = app.MapGroup("/api/time").RequireAuthorization();
        
        time.MapGet("/holidays", GetHolidays);
        time.MapPost("/holidays", AddHoliday);
        time.MapGet("/leave-policies", GetLeavePolicies);
        time.MapPost("/leave-policies", CreateLeavePolicy);
        time.MapGet("/leave-balances", GetLeaveBalances);
        time.MapPost("/leave-requests/calculate", CalculateLeaveDays);
        time.MapPost("/leave-requests", SubmitLeaveRequest);
        time.MapGet("/leave-requests/pending", GetPendingLeaveRequests);
        time.MapPut("/leave-requests/{id}/approve", ApproveLeaveRequest);
        time.MapPut("/leave-requests/{id}/reject", RejectLeaveRequest);
        time.MapGet("/timesheets/current-week", GetCurrentWeekTimesheet);
        time.MapPost("/timesheets", SubmitTimesheet);
        time.MapGet("/timesheets/pending", GetPendingTimesheets);
        time.MapPut("/timesheets/{id}/approve", ApproveTimesheet);
        time.MapPut("/timesheets/{id}/reject", RejectTimesheet);

        return app;
    }

    // --- DLQ & Fraud ---

    private static async Task<IResult> GetDlq(TimeAttendanceDbContext dbContext)
    {
        var pending = await dbContext.InvalidPunches
            .Where(p => p.Status == Karamchari.TimeAttendance.Domain.IoT.InvalidPunchStatus.Pending)
            .OrderByDescending(p => p.TimestampUtc)
            .ToListAsync();
        return Results.Ok(pending);
    }

    private static async Task<IResult> MapDlq(
        Guid id,
        MapDlqRequest request,
        TimeAttendanceDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        ITenantProvider tenantProvider)
    {
        var punch = await dbContext.InvalidPunches.FindAsync(id);
        if (punch == null) return Results.NotFound();

        var tenant = tenantProvider.GetTenant();
        
        string? biometricId = null;
        try {
            using var document = System.Text.Json.JsonDocument.Parse(punch.Payload);
            biometricId = document.RootElement.TryGetProperty("biometricId", out var prop) ? prop.GetString() : null;
        } catch { }

        if (string.IsNullOrEmpty(biometricId)) return Results.BadRequest("Invalid punch payload: missing biometricId");

        using var tx = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var mapping = Karamchari.TimeAttendance.Domain.IoT.BiometricMapping.Create(
                tenant.TenantId, biometricId ?? string.Empty, request.EmployeeId);
            dbContext.BiometricMappings.Add(mapping);

            punch.TransitionTo(Karamchari.TimeAttendance.Domain.IoT.InvalidPunchStatus.Mapped);

            var job = new Karamchari.TimeAttendance.Domain.IoT.BackgroundJob { 
                TenantId = tenant.TenantId, 
                JobType = "ReprocessAttendance" 
            };
            dbContext.BackgroundJobs.Add(job);

            await publishEndpoint.Publish(new Karamchari.Core.Contracts.IntegrationEvents.ReprocessAttendanceCommandV1
            {
                JobId = job.Id,
                TenantId = tenant.TenantId,
                EmployeeId = request.EmployeeId,
                FromDateUtc = punch.TimestampUtc.Date,
                ToDateUtc = punch.TimestampUtc.Date
            });

            await dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return Results.Ok(new { JobId = job.Id });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static async Task<IResult> GetFraud(TimeAttendanceDbContext dbContext)
    {
        var flags = await dbContext.FraudFlags
            .Where(f => f.Status == Karamchari.TimeAttendance.Domain.IoT.FraudStatus.Detected || f.Status == Karamchari.TimeAttendance.Domain.IoT.FraudStatus.UnderReview)
            .OrderByDescending(f => f.SeverityScore)
            .ThenByDescending(f => f.DetectedAtUtc)
            .ToListAsync();
        return Results.Ok(flags);
    }

    private static async Task<IResult> ReviewFraud(
        Guid id,
        ReviewFraudRequest request,
        TimeAttendanceDbContext dbContext,
        ClaimsPrincipal user)
    {
        var flag = await dbContext.FraudFlags.FindAsync(id);
        if (flag == null) return Results.NotFound();

        var reviewer = user.Identity?.Name ?? "Admin";
        flag.Review(request.Status, reviewer, request.Notes);

        await dbContext.SaveChangesAsync();
        return Results.Ok();
    }

    private static async Task<IResult> Reprocess(
        ReprocessAttendanceRequest request,
        IPublishEndpoint publishEndpoint,
        ITenantProvider tenantProvider)
    {
        if (!DateTime.TryParse(request.FromDate, out var fromDate) || 
            !DateTime.TryParse(request.ToDate, out var toDate))
        {
            return Results.BadRequest("Invalid date format. Use ISO8601.");
        }

        var tenant = tenantProvider.GetTenant();
        var jobId = Guid.NewGuid();

        await publishEndpoint.Publish(new Karamchari.Core.Contracts.IntegrationEvents.ReprocessAttendanceCommandV1
        {
            JobId = jobId,
            TenantId = tenant.TenantId,
            EmployeeId = request.EmployeeId ?? Guid.Empty,
            FromDateUtc = fromDate.ToUniversalTime(),
            ToDateUtc = toDate.ToUniversalTime()
        });

        return Results.Ok(new { JobId = jobId, Message = "Reprocessing queued successfully." });
    }

    private static async Task<IResult> GetReprocessJob(Guid jobId, TimeAttendanceDbContext db)
    {
        var job = await db.BackgroundJobs.FindAsync(jobId);
        return job != null ? Results.Ok(job) : Results.NotFound();
    }

    // --- Holidays & Leaves ---

    private static async Task<IResult> GetHolidays(TimeAttendanceDbContext dbContext)
    {
        var holidays = await dbContext.HolidayCalendars
            .SelectMany(c => c.Holidays)
            .OrderBy(h => h.Date)
            .ToListAsync();
        return Results.Ok(holidays);
    }

    private static async Task<IResult> AddHoliday(AddHolidayRequest request, TimeAttendanceDbContext dbContext)
    {
        var calendar = await dbContext.HolidayCalendars
            .Include(c => c.Holidays)
            .FirstOrDefaultAsync(c => c.Name == "Default Calendar");
            
        if (calendar == null)
        {
            calendar = HolidayCalendar.Create("Default Calendar", "Standard Organizational Calendar");
            dbContext.HolidayCalendars.Add(calendar);
        }
        
        calendar.AddHoliday(request.Date, request.Name);
        await dbContext.SaveChangesAsync();
        
        var newHoliday = calendar.Holidays.First(h => h.Date == request.Date);
        return Results.Created($"/api/time/holidays/{newHoliday.Id}", newHoliday);
    }

    private static async Task<IResult> GetLeavePolicies(TimeAttendanceDbContext dbContext)
    {
        var policies = await dbContext.LeavePolicies
            .Where(p => p.IsActive)
            .ToListAsync();
        return Results.Ok(policies);
    }

    private static async Task<IResult> CreateLeavePolicy(CreateLeavePolicyRequest request, TimeAttendanceDbContext dbContext)
    {
        var policy = LeavePolicy.Create(request.Name, request.Description, request.Rules);
        dbContext.LeavePolicies.Add(policy);
        await dbContext.SaveChangesAsync();
        return Results.Created($"/api/time/leave-policies/{policy.Id}", policy);
    }

    private static async Task<IResult> GetLeaveBalances(TimeAttendanceDbContext dbContext)
    {
        var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var balances = await dbContext.LeaveBalances
            .Where(b => b.EmployeeId == employeeId)
            .ToListAsync();
        return Results.Ok(balances);
    }

    private static async Task<IResult> CalculateLeaveDays(
        CalculateLeaveDaysRequest request, 
        TimeAttendanceDbContext dbContext)
    {
        var policy = await dbContext.LeavePolicies.FindAsync(request.PolicyId);
        if (policy == null) return Results.NotFound("Policy not found");

        var holidays = await dbContext.HolidayCalendars
            .SelectMany(c => c.Holidays)
            .Where(h => h.Date >= request.StartDate && h.Date <= request.EndDate)
            .Select(h => h.Date)
            .ToListAsync();

        var days = LeaveRequestService.CalculateActualLeaveDays(
            request.StartDate, 
            request.EndDate, 
            holidays, 
            policy.Rules.AllowHalfDays);

        return Results.Ok(new { ActualDays = days });
    }

    private static async Task<IResult> SubmitLeaveRequest(
        SubmitLeaveRequestRequest request, 
        TimeAttendanceDbContext dbContext,
        IPublishEndpoint publishEndpoint)
    {
        var policy = await dbContext.LeavePolicies.FindAsync(request.PolicyId);
        if (policy == null) return Results.BadRequest("Policy not found");

        var holidays = await dbContext.HolidayCalendars
            .SelectMany(c => c.Holidays)
            .Where(h => h.Date >= request.StartDate && h.Date <= request.EndDate)
            .Select(h => h.Date)
            .ToListAsync();

        var actualDays = LeaveRequestService.CalculateActualLeaveDays(
            request.StartDate, 
            request.EndDate, 
            holidays, 
            policy.Rules.AllowHalfDays);
            
        if (actualDays <= 0) return Results.BadRequest("No working days in range");

        var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001"); 
        
        var balance = await dbContext.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.PolicyId == request.PolicyId);
            
        if (balance == null)
        {
            balance = LeaveBalance.Create(employeeId, request.PolicyId, policy.Rules.AnnualAllowance);
            dbContext.LeaveBalances.Add(balance);
        }

        if (balance.RemainingBalance < actualDays) 
        {
            return Results.BadRequest($"Insufficient balance. Required: {actualDays}, Available: {balance.RemainingBalance}");
        }

        balance.Deduct(actualDays);

        var leaveRequest = LeaveRequest.Create(
            employeeId, 
            request.PolicyId, 
            request.StartDate, 
            request.EndDate, 
            actualDays, 
            request.Reason);

        if (!policy.Rules.RequiresManagerApproval)
        {
            leaveRequest.Approve();
            
            await publishEndpoint.Publish(new Karamchari.Core.Contracts.IntegrationEvents.LeaveRequestApprovedIntegrationEvent(
                leaveRequest.Id, 
                employeeId, 
                leaveRequest.StartDate, 
                leaveRequest.EndDate, 
                actualDays, 
                policy.Rules.Category == LeaveCategory.Paid));
        }

        dbContext.LeaveRequests.Add(leaveRequest);
        await dbContext.SaveChangesAsync();

        return Results.Created($"/api/time/leave-requests/{leaveRequest.Id}", leaveRequest);
    }

    private static async Task<IResult> GetPendingLeaveRequests(TimeAttendanceDbContext dbContext)
    {
        var requests = await dbContext.LeaveRequests
            .Where(r => r.Status == LeaveRequestStatus.Pending)
            .OrderByDescending(r => r.RequestedOnUtc)
            .ToListAsync();
        return Results.Ok(requests);
    }

    private static async Task<IResult> ApproveLeaveRequest(
        Guid id, 
        TimeAttendanceDbContext dbContext,
        IPublishEndpoint publishEndpoint)
    {
        var request = await dbContext.LeaveRequests.FindAsync(id);
        if (request == null) return Results.NotFound();

        var policy = await dbContext.LeavePolicies.FindAsync(request.PolicyId);
        if (policy == null) return Results.BadRequest("Policy not found.");

        request.Approve();
        
        await publishEndpoint.Publish(new Karamchari.Core.Contracts.IntegrationEvents.LeaveRequestApprovedIntegrationEvent(
            request.Id,
            request.EmployeeId,
            request.StartDate,
            request.EndDate,
            request.ActualDays,
            policy.Rules.Category == LeaveCategory.Paid));

        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> RejectLeaveRequest(
        Guid id, 
        TimeAttendanceDbContext dbContext)
    {
        var request = await dbContext.LeaveRequests.FindAsync(id);
        if (request == null) return Results.NotFound();

        var balance = await dbContext.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == request.EmployeeId && b.PolicyId == request.PolicyId);
            
        balance?.Refund(request.ActualDays);

        request.Reject();
        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }

    // --- Timesheets ---

    private static async Task<IResult> GetCurrentWeekTimesheet(
        TimeAttendanceDbContext dbContext,
        TimeProvider timeProvider)
    {
        var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().DateTime);
        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        var weekStart = today.AddDays(-1 * diff);
        
        var timesheet = await dbContext.Timesheets
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.WeekStartDate == weekStart);
            
        if (timesheet != null) return Results.Ok(timesheet);
        
        var blankEntries = Enumerable.Range(0, 7)
            .Select(i => new TimeEntry { Date = weekStart.AddDays(i), Hours = 0, Description = null })
            .ToList();
            
        return Results.Ok(new 
        { 
            EmployeeId = employeeId,
            WeekStartDate = weekStart,
            Status = TimesheetStatus.Draft,
            Entries = blankEntries,
            TotalHours = 0
        });
    }

    private static async Task<IResult> SubmitTimesheet(
        SubmitTimesheetRequest request,
        ClaimsPrincipal user,
        TimeAttendanceDbContext dbContext)
    {
        var employeeId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var uid)
            ? uid
            : Guid.Parse("00000000-0000-0000-0000-000000000001");

        var timesheet = await dbContext.Timesheets
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.WeekStartDate == request.WeekStartDate);

        if (timesheet == null)
        {
            timesheet = Timesheet.Create(employeeId, request.WeekStartDate);
            dbContext.Timesheets.Add(timesheet);
        }

        var entries = request.Entries.Select(e => new TimeEntry
        {
            Date = e.Date,
            Hours = e.Hours,
            Description = e.Description
        });

        timesheet.UpdateEntries(entries);
        timesheet.Submit(employeeId);

        await dbContext.SaveChangesAsync();
        return Results.Ok(timesheet);
    }

    private static async Task<IResult> GetPendingTimesheets(TimeAttendanceDbContext dbContext)
    {
        var timesheets = await dbContext.Timesheets
            .Where(t => t.Status == TimesheetStatus.Submitted)
            .OrderByDescending(t => t.WeekStartDate)
            .ToListAsync();
        return Results.Ok(timesheets);
    }

    private static async Task<IResult> ApproveTimesheet(
        Guid id,
        ClaimsPrincipal user,
        TimesheetService timesheetService)
    {
        var approverId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var uid)
            ? uid
            : Guid.Empty;

        try
        {
            await timesheetService.ApproveAsync(id, approverId);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> RejectTimesheet(
        Guid id, 
        RejectTimesheetRequest request,
        TimeAttendanceDbContext dbContext)
    {
        var timesheet = await dbContext.Timesheets.FindAsync(id);
        if (timesheet == null) return Results.NotFound();

        timesheet.Reject(request.Reason, Guid.Empty);
        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }
}
