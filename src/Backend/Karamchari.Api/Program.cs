using Karamchari.Api;
using Karamchari.Core.DependencyInjection;
using Karamchari.HR.DependencyInjection;
using Karamchari.Payroll.DependencyInjection;
using Karamchari.TimeAttendance.DependencyInjection;
using Karamchari.Payroll.Contracts;
using MassTransit;
using Karamchari.HR.Persistence;
using Karamchari.Payroll.Data;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.TimeAttendance.Domain.Holidays;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Domain.Timesheets;
using Karamchari.TimeAttendance.Contracts;
using Karamchari.Core.Contracts;
using Microsoft.EntityFrameworkCore;

// Entry point for the Karamchari API.
// Composes all bounded contexts and shared infrastructure.
var builder = WebApplication.CreateBuilder(args);

// Add Core Infrastructure (Multitenancy, Interceptors)
builder.Services.AddKaramchariCore(builder.Configuration);
builder.Services.AddScoped<Karamchari.Core.Persistence.Provisioning.TenantProvisioningService>(sp => 
    new Karamchari.Core.Persistence.Provisioning.TenantProvisioningService(
        sp.GetRequiredService<HRDbContext>(), 
        sp.GetRequiredService<Karamchari.Core.Persistence.Provisioning.ITenantTableRegistry>(),
        sp.GetRequiredService<Karamchari.Core.Persistence.Provisioning.RlsScriptGenerator>(),
        sp.GetRequiredService<ILogger<Karamchari.Core.Persistence.Provisioning.TenantProvisioningService>>()));

// Configure MassTransit with Transactional Outbox and Module Sagas
builder.Services.AddMassTransit(x =>
{
    // Add Bounded Contexts (now passing MassTransit configurator)
    builder.Services.AddKaramchariHR(builder.Configuration, x);
    builder.Services.AddKaramchariTimeAttendance(builder.Configuration, x);
    builder.Services.AddKaramchariPayroll(builder.Configuration, x);

    // Transactional Outbox for HR Context
    x.AddEntityFrameworkOutbox<HRDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();
    });

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// --- Tenant Provisioning API ---

// Provisions a new tenant, creates their schema/tables, and seeds default data.
app.MapPost("/api/tenants", async (
    ProvisionTenantRequest request, 
    Karamchari.Core.Persistence.Provisioning.TenantProvisioningService provisioningService,
    IPublishEndpoint publishEndpoint) =>
{
    // 1. Generate unique tenant and schema identifiers
    var tenantId = request.CompanyName.ToLowerInvariant().Replace(" ", "_");
    var schemaName = $"tenant_{tenantId}";
    
    // 2. Physical Provisioning (Schema, Table structures, RLS)
    await provisioningService.ProvisionTenantAsync(tenantId, schemaName);
    
    // 3. Trigger Distributed Module Seeding (Asynchronous)
    await publishEndpoint.Publish(new TenantProvisionedIntegrationEvent(
        tenantId, 
        request.CompanyName, 
        request.AdminEmail));
    
    return Results.Created($"/api/tenants/{tenantId}", new { TenantId = tenantId, SchemaName = schemaName });
});

// --- Payroll API ---

// Initiates a new payroll run saga.
app.MapPost("/api/payroll/runs", async (StartPayrollRunRequest request, IPublishEndpoint publishEndpoint) =>
{
    // Generate a COMB GUID for better SQL Server indexing performance
    var runId = NewId.NextGuid();
    
    // In this modular monolith, the tenant is resolved automatically by HttpTenantProvider
    // but for the command we pass it explicitly so the saga knows its home.
    var tenantId = "tenant_oakridge"; 
    
    await publishEndpoint.Publish(new StartPayrollRunCommand(runId, tenantId, request.PeriodName));
    
    return Results.Accepted($"/api/payroll/runs/{runId}", new { RunId = runId });
});

// Returns all payroll runs for the current tenant.
// RLS and the Schema Interceptor handle the filtering automatically.
app.MapGet("/api/payroll/runs", async (PayrollDbContext dbContext) =>
{
    var runs = await dbContext.PayrollRunStates
        .OrderByDescending(x => x.StartedAt)
        .ToListAsync();
        
    return Results.Ok(runs);
});

// --- Time & Attendance API ---

// Returns all holidays for the current tenant.
app.MapGet("/api/time/holidays", async (TimeAttendanceDbContext dbContext) =>
{
    // Fetch all holidays across all calendars (RLS automatically filters by tenant).
    // In a mature system, we would filter by the specific calendar assigned to the user's region.
    var holidays = await dbContext.HolidayCalendars
        .SelectMany(c => c.Holidays)
        .OrderBy(h => h.Date)
        .ToListAsync();
        
    return Results.Ok(holidays);
});

// Adds a new holiday to the default organizational calendar.
app.MapPost("/api/time/holidays", async (AddHolidayRequest request, TimeAttendanceDbContext dbContext) =>
{
    // Find or create the "Default Calendar" for the tenant.
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
    
    // Find the newly added holiday to return its ID
    var newHoliday = calendar.Holidays.First(h => h.Date == request.Date);
    
    return Results.Created($"/api/time/holidays/{newHoliday.Id}", newHoliday);
});

// Returns all leave policies for the current tenant.
app.MapGet("/api/time/leave-policies", async (TimeAttendanceDbContext dbContext) =>
{
    var policies = await dbContext.LeavePolicies
        .Where(p => p.IsActive)
        .ToListAsync();
        
    return Results.Ok(policies);
});

// Creates a new leave policy.
app.MapPost("/api/time/leave-policies", async (CreateLeavePolicyRequest request, TimeAttendanceDbContext dbContext) =>
{
    var policy = LeavePolicy.Create(request.Name, request.Description, request.Rules);
    dbContext.LeavePolicies.Add(policy);
    await dbContext.SaveChangesAsync();
    
    return Results.Created($"/api/time/leave-policies/{policy.Id}", policy);
});

// Returns leave balances for the current user (simulated).
app.MapGet("/api/time/leave-balances", async (TimeAttendanceDbContext dbContext) =>
{
    // In this sprint, we resolve EmployeeId from context (mocked for now).
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    var balances = await dbContext.LeaveBalances
        .Where(b => b.EmployeeId == employeeId)
        .ToListAsync();
        
    return Results.Ok(balances);
});

// Calculates the actual leave days for a given range (dry-run for UI feedback).
app.MapPost("/api/time/leave-requests/calculate", async (
    CalculateLeaveDaysRequest request, 
    TimeAttendanceDbContext dbContext,
    LeaveRequestService leaveService) =>
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
});

// Submits a new leave request.
app.MapPost("/api/time/leave-requests", async (
    SubmitLeaveRequestRequest request, 
    TimeAttendanceDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    LeaveRequestService leaveService) =>
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

    // Mock current user
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

    // 1. Deduct balance immediately (Reserve)
    balance.Deduct(actualDays);

    var leaveRequest = LeaveRequest.Create(
        employeeId, 
        request.PolicyId, 
        request.StartDate, 
        request.EndDate, 
        actualDays, 
        request.Reason);

    // 2. If no approval needed, move to Approved and emit event
    if (!policy.Rules.RequiresManagerApproval)
    {
        leaveRequest.Approve();
        
        await publishEndpoint.Publish(new LeaveRequestApprovedIntegrationEvent(
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
});

// Returns all pending leave requests for the current tenant.
app.MapGet("/api/time/leave-requests/pending", async (TimeAttendanceDbContext dbContext) =>
{
    var requests = await dbContext.LeaveRequests
        .Where(r => r.Status == LeaveRequestStatus.Pending)
        .OrderByDescending(r => r.RequestedOnUtc)
        .ToListAsync();
        
    return Results.Ok(requests);
});

// Approves a leave request.
app.MapPut("/api/time/leave-requests/{id}/approve", async (
    Guid id, 
    TimeAttendanceDbContext dbContext,
    IPublishEndpoint publishEndpoint) =>
{
    var request = await dbContext.LeaveRequests.FindAsync(id);
    if (request == null) return Results.NotFound();

    var policy = await dbContext.LeavePolicies.FindAsync(request.PolicyId);
    if (policy == null) return Results.BadRequest("Policy not found.");

    request.Approve();
    
    // Publish integration event for Payroll module
    await publishEndpoint.Publish(new LeaveRequestApprovedIntegrationEvent(
        request.Id,
        request.EmployeeId,
        request.StartDate,
        request.EndDate,
        request.ActualDays,
        policy.Rules.Category == LeaveCategory.Paid));

    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});

// Rejects a leave request.
app.MapPut("/api/time/leave-requests/{id}/reject", async (
    Guid id, 
    TimeAttendanceDbContext dbContext) =>
{
    var request = await dbContext.LeaveRequests.FindAsync(id);
    if (request == null) return Results.NotFound();

    // Refund the balance that was reserved upon submission
    var balance = await dbContext.LeaveBalances
        .FirstOrDefaultAsync(b => b.EmployeeId == request.EmployeeId && b.PolicyId == request.PolicyId);
        
    balance?.Refund(request.ActualDays);

    request.Reject();
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});

// Returns the current week's timesheet for the authenticated employee.
app.MapGet("/api/time/timesheets/current-week", async (
    TimeAttendanceDbContext dbContext,
    TimeProvider timeProvider) =>
{
    // Mock current user
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    
    var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().DateTime);
    // Find Monday of the current week
    int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
    var weekStart = today.AddDays(-1 * diff);
    
    var timesheet = await dbContext.Timesheets
        .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.WeekStartDate == weekStart);
        
    if (timesheet != null)
    {
        return Results.Ok(timesheet);
    }
    
    // Return a blank draft template if no timesheet exists for this week
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
});

// Submits or updates a weekly timesheet.
app.MapPost("/api/time/timesheets", async (
    SubmitTimesheetRequest request, 
    TimeAttendanceDbContext dbContext) =>
{
    // Mock current user
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    
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
    timesheet.Submit(); // Auto-submit for now to trigger manager workflow later
    
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(timesheet);
});

// Returns all pending timesheets for manager review.
app.MapGet("/api/time/timesheets/pending", async (TimeAttendanceDbContext dbContext) =>
{
    var timesheets = await dbContext.Timesheets
        .Where(t => t.Status == TimesheetStatus.Submitted)
        .OrderByDescending(t => t.WeekStartDate)
        .ToListAsync();
        
    return Results.Ok(timesheets);
});

// Approves a weekly timesheet.
app.MapPut("/api/time/timesheets/{id}/approve", async (
    Guid id, 
    TimeAttendanceDbContext dbContext,
    IPublishEndpoint publishEndpoint) =>
{
    var timesheet = await dbContext.Timesheets.FindAsync(id);
    if (timesheet == null) return Results.NotFound();

    timesheet.Approve();
    
    // Publish integration event for Payroll context to record hours in the ledger
    await publishEndpoint.Publish(new TimesheetApprovedIntegrationEvent(
        timesheet.Id,
        timesheet.EmployeeId,
        timesheet.WeekStartDate,
        timesheet.TotalHours));

    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});

// Rejects a weekly timesheet.
app.MapPut("/api/time/timesheets/{id}/reject", async (
    Guid id, 
    RejectTimesheetRequest request,
    TimeAttendanceDbContext dbContext) =>
{
    var timesheet = await dbContext.Timesheets.FindAsync(id);
    if (timesheet == null) return Results.NotFound();

    timesheet.Reject(request.Reason);
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

namespace Karamchari.Api
{
    /// <summary>
    /// Request contract for starting a payroll run.
    /// </summary>
    /// <param name="PeriodName">The name of the payroll period (e.g., "April 2027").</param>
    public record StartPayrollRunRequest(string PeriodName);

    /// <summary>
    /// Request contract for adding a new holiday.
    /// </summary>
    /// <param name="Name">The name of the holiday (e.g., "Independence Day").</param>
    /// <param name="Date">The date of the holiday.</param>
    public record AddHolidayRequest(string Name, DateOnly Date);

    /// <summary>
    /// Request contract for creating a new leave policy.
    /// </summary>
    /// <param name="Name">The name.</param>
    /// <param name="Description">The description.</param>
    /// <param name="Rules">The rules.</param>
    public record CreateLeavePolicyRequest(string Name, string Description, LeavePolicyRules Rules);

    /// <summary>
    /// Request contract for submitting a leave request.
    /// </summary>
    public record SubmitLeaveRequestRequest(Guid PolicyId, DateOnly StartDate, DateOnly EndDate, string? Reason);

    /// <summary>
    /// Request contract for dry-running leave day calculation.
    /// </summary>
    /// <param name="PolicyId">The policy identifier.</param>
    /// <param name="StartDate">The start date.</param>
    /// <param name="EndDate">The end date.</param>
    public record CalculateLeaveDaysRequest(Guid PolicyId, DateOnly StartDate, DateOnly EndDate);

    /// <summary>
    /// Request contract for provisioning a new tenant.
    /// </summary>
    /// <param name="CompanyName">The name of the company.</param>
    /// <param name="AdminEmail">The email address of the primary administrator.</param>
    public record ProvisionTenantRequest(string CompanyName, string AdminEmail);

    /// <summary>
    /// Request contract for submitting a weekly timesheet.
    /// </summary>
    /// <param name="WeekStartDate">The start of the week.</param>
    /// <param name="Entries">The list of daily time entries.</param>
    public record SubmitTimesheetRequest(DateOnly WeekStartDate, List<TimeEntryDto> Entries);

    /// <summary>
    /// DTO for a single time entry in a request.
    /// </summary>
    /// <param name="Date">The date.</param>
    /// <param name="Hours">The billable hours.</param>
    /// <param name="Description">The work description.</param>
    public record TimeEntryDto(DateOnly Date, decimal Hours, string? Description);

    /// <summary>
    /// Request contract for rejecting a timesheet.
    /// </summary>
    /// <param name="Reason">The reason for rejection.</param>
    public record RejectTimesheetRequest(string Reason);
}
