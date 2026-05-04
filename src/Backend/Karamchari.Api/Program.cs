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
using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services.Statutory.Rules;
using Karamchari.Payroll.Services.Declarations;
using Karamchari.TimeAttendance.Contracts;
using Karamchari.Core.Contracts;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

// Entry point for the Karamchari API.
var builder = WebApplication.CreateBuilder(args);

// Rate Limiting Configuration
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ai", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromSeconds(10);
        opt.QueueLimit = 2;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddFixedWindowLimiter("ess", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromSeconds(1);
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

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

    // Transactional Outbox — one per DbContext that publishes domain/integration events.
    // Outbox tables (InboxState, OutboxMessage, OutboxState) are pinned to dbo (shared infra, not tenant-owned).
    x.AddEntityFrameworkOutbox<HRDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();
    });
    x.AddEntityFrameworkOutbox<PayrollDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();
    });
    x.AddEntityFrameworkOutbox<TimeAttendanceDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();
    });

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);

        // Global Concurrency and Retry Policy
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        
        // Bounded Concurrency: Limits simultaneous processing to protect the DB
        // In production, this would be tuned based on CPU cores and DB capacity
        cfg.ConcurrentMessageLimit = 8; 
    });
});

var app = builder.Build();

app.UseRateLimiter();

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
app.MapPost("/api/payroll/runs", async (StartPayrollRunRequest request, IPublishEndpoint publishEndpoint, ITenantProvider tenantProvider) =>
{
    // Generate a COMB GUID for better SQL Server indexing performance
    var runId = NewId.NextGuid();

    // Resolve tenant from the authenticated request (JWT or gateway header)
    var tenant = tenantProvider.GetTenant();

    await publishEndpoint.Publish(new StartPayrollRunCommand(runId, tenant.TenantId, request.PeriodName));

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

// Returns a high-level summary of a specific payroll run (The "CFO View").
app.MapGet("/api/payroll/runs/{id}/summary", async (Guid id, PayrollDbContext dbContext) =>
{
    var run = await dbContext.PayrollRunStates.FirstOrDefaultAsync(r => r.CorrelationId == id);
    if (run == null) return Results.NotFound();

    // In a real app, we might also want to aggregate these from the ledger for double-verification
    return Results.Ok(new 
    {
        run.CorrelationId,
        run.PeriodName,
        run.CurrentState,
        run.TotalEmployeesToProcess,
        run.ProcessedEmployees,
        run.TotalGross,
        run.TotalNet,
        TotalTax = run.TotalGross - run.TotalNet, // Simplified tax aggregation
        run.StartedAt,
        run.LockedAt,
        run.LockedBy
    });
});

// Returns a detailed, itemized list of all employees in the run with anomaly detection.
app.MapGet("/api/payroll/runs/{id}/details", async (Guid id, PayrollDbContext dbContext) =>
{
    var run = await dbContext.PayrollRunStates.FirstOrDefaultAsync(r => r.CorrelationId == id);
    if (run == null) return Results.NotFound();

    // Fetch current run entries
    var currentEntries = await dbContext.PayrollLedger
        .Where(e => e.RunId == id)
        .ToListAsync();

    var employeeIds = currentEntries.Select(e => e.EmployeeId).ToList();

    // Fetch previous run entries for variance calculation
    // Simplified: Look for entries from the same month last year or previous month
    // For now, let's just find the most recent entry for these employees that is NOT this run
    var previousEntries = await dbContext.PayrollLedger
        .Where(e => employeeIds.Contains(e.EmployeeId) && e.RunId != id)
        .GroupBy(e => e.EmployeeId)
        .Select(g => g.OrderByDescending(e => e.Year).ThenByDescending(e => e.Month).First())
        .ToListAsync();

    var previousMap = previousEntries.ToDictionary(e => e.EmployeeId, e => e);

    var details = currentEntries.Select(e => 
    {
        var prev = previousMap.GetValueOrDefault(e.EmployeeId);
        decimal variance = 0;
        if (prev != null && prev.NetPay != 0)
        {
            variance = (e.NetPay - prev.NetPay) / prev.NetPay;
        }

        return new
        {
            e.EmployeeId,
            e.MonthlyGross,
            e.NetPay,
            TDS = e.TdsDeducted,
            VariancePercentage = variance,
            IsAnomaly = Math.Abs(variance) > 0.10m // 10% threshold
        };
    });

    return Results.Ok(details);
});

// Locks a payroll run and triggers payslip generation.
app.MapPut("/api/payroll/runs/{id}/lock", async (Guid id, LockPayrollRunRequest request, IPublishEndpoint publishEndpoint) =>
{
    // The Saga handles the state transition and integration event firing
    await publishEndpoint.Publish(new LockPayrollRunCommand(id, request.ApprovedBy));
    
    return Results.Accepted();
});

// --- Salary Structure API ---

// Creates a new master salary component.
app.MapPost("/api/payroll/salary-components", async (CreateSalaryComponentRequest request, PayrollDbContext dbContext) =>
{
    var component = SalaryComponent.Create(request.Name, request.Type, request.Rounding);
    dbContext.SalaryComponents.Add(component);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/api/payroll/salary-components/{component.Id}", component);
});

// Returns all salary components for the current tenant.
app.MapGet("/api/payroll/salary-components", async (PayrollDbContext dbContext) =>
{
    var components = await dbContext.SalaryComponents.ToListAsync();
    return Results.Ok(components);
});

// Creates a new salary template.
app.MapPost("/api/payroll/salary-templates", async (CreateSalaryTemplateRequest request, PayrollDbContext dbContext) =>
{
    var template = SalaryTemplate.Create(request.Name);
    foreach (var comp in request.Components)
    {
        template.AddComponent(comp);
    }
    dbContext.SalaryTemplates.Add(template);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/api/payroll/salary-templates/{template.Id}", template);
});

// Dry-runs a CTC breakdown calculation based on a template.
app.MapPost("/api/payroll/salary-templates/{id}/calculate", async (
    Guid id, 
    CalculateCTCBreakdownRequest request, 
    PayrollDbContext dbContext) =>
{
    var template = await dbContext.SalaryTemplates.FindAsync(id);
    if (template == null) return Results.NotFound("Template not found");
    
    var masterComponents = await dbContext.SalaryComponents.ToListAsync();
    
    // In a high-throughput system, this CompiledExecutionPlan would be cached in Redis/Memory.
    var plan = CTCTemplateCompiler.Compile(template, masterComponents);
    var result = CTCBreakdownService.Calculate(request.AnnualCTC, plan, request.Overrides);
    
    return Results.Ok(result);
});

// Performs a full salary calculation including statutory deductions (EPF, ESIC, PT, TDS).
app.MapPost("/api/payroll/salary-templates/{id}/calculate-statutory", async (
    Guid id,
    CalculateStatutoryRequest request,
    PayrollDbContext dbContext,
    IProfessionalTaxProvider ptProvider,
    IIncomeProjectionService projectionService,
    IExemptionCalculator exemptionCalculator,
    ITaxSlabProvider taxSlabProvider,
    IITDeclarationRepository declarationRepository) =>
{
    var template = await dbContext.SalaryTemplates.FindAsync(id);
    if (template == null) return Results.NotFound("Template not found");

    var masterComponents = await dbContext.SalaryComponents.ToListAsync();
    var plan = CTCTemplateCompiler.Compile(template, masterComponents);
    var breakdown = CTCBreakdownService.Calculate(request.AnnualCTC, plan, request.Overrides);

    // Load profile or use defaults for the dry-run
    var profile = request.EmployeeId.HasValue
        ? await dbContext.PayrollProfiles.FirstOrDefaultAsync(p => p.EmployeeId == request.EmployeeId.Value)
        : PayrollProfile.CreateDraft(Guid.Empty);

    // In a real app, the rule set would be resolved via a factory based on the current date
    var ruleSet = new FY20262027RuleSet(
        request.EpfBaseComponentIds,
        ptProvider,
        projectionService,
        exemptionCalculator,
        taxSlabProvider,
        declarationRepository);

    var statutoryContext = new StatutoryContext(breakdown, profile!, ruleSet.Year, DateTime.UtcNow.Month);
    
    var result = await StatutoryPipelineEngine.ExecuteAsync(statutoryContext, ruleSet);
    
    return Results.Ok(new 
    {
        Breakdown = breakdown,
        Statutory = result
    });
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

// --- Employee Self-Service (ESS) API ---

// Returns all finalized payslips for the authenticated employee.
app.MapGet("/api/ess/payslips", async (PayrollDbContext dbContext) =>
{
    // In a production app, this would be resolved from the JWT claim.
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    var payslips = await dbContext.PayrollLedger
        .Where(e => e.EmployeeId == employeeId)
        .OrderByDescending(e => e.Year)
        .ThenByDescending(e => e.Month)
        .Select(e => new
        {
            e.Id,
            e.Year,
            e.Month,
            e.PeriodName,
            e.MonthlyGross,
            e.NetPay,
            e.TdsDeducted
        })
        .ToListAsync();

    return Results.Ok(payslips);
});

// Streams a payslip PDF for the authenticated employee.
// Security: Deterministic path prevents IDOR/Enumeration attacks.
app.MapGet("/api/ess/payslips/{year}/{month}/download", async (
    int year, 
    int month, 
    PayrollDbContext dbContext,
    IPayslipStorage storage) =>
{
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    // 1. Verify the ledger entry exists for this specific user (Ownership Check)
    var ledger = await dbContext.PayrollLedger
        .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.Year == year && e.Month == month);

    if (ledger == null)
    {
        return Results.NotFound("Payslip not found or access denied.");
    }

    try
    {
        // 2. Fetch from storage via proxy (Security: Never expose internal storage paths)
        var pdfBytes = await storage.GetAsync(employeeId.ToString(), ledger.PeriodName);
        
        var fileName = $"Payslip_{ledger.PeriodName.Replace(" ", "_")}.pdf";
        return Results.File(pdfBytes, "application/pdf", fileName);
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound("Physical payslip file missing.");
    }
});

// Returns the Year-to-Date (YTD) summary for the current financial year.
app.MapGet("/api/ess/payslips/ytd", async (PayrollDbContext dbContext) =>
{
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    
    // Determine the current Indian Financial Year (April 1st to March 31st)
    var now = DateTime.UtcNow;
    var fyStartYear = now.Month >= 4 ? now.Year : now.Year - 1;

    // Batch query with aggregation (Performance: DB-side summation)
    var ytd = await dbContext.PayrollLedger
        .Where(e => e.EmployeeId == employeeId && e.FinancialYearStart == fyStartYear)
        .GroupBy(e => e.EmployeeId)
        .Select(g => new
        {
            GrossYtd = g.Sum(x => x.MonthlyGross),
            NetYtd = g.Sum(x => x.NetPay),
            TaxYtd = g.Sum(x => x.TdsDeducted),
            Count = g.Count()
        })
        .FirstOrDefaultAsync();

    return Results.Ok(ytd ?? new { GrossYtd = 0, NetYtd = 0, TaxYtd = 0, Count = 0 });
});

// --- Tax Simulator API ---

// Performs a stateless "What-If" tax projection comparison.
app.MapPost("/api/ess/tax-simulator/dry-run", async (
    TaxSimulationRequest request,
    PayrollDbContext dbContext,
    IServiceProvider sp) =>
{
    // 1. Security & Validation
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    if (request.Section80C > 150000) return Results.BadRequest("Section 80C declaration cannot exceed ₹1,50,000.");
    if (request.MonthlyRent < 0) return Results.BadRequest("Monthly rent cannot be negative.");

    // 2. Load Base Context (Profile + Salary Template)
    var profile = await dbContext.PayrollProfiles.FirstOrDefaultAsync(p => p.EmployeeId == employeeId);
    if (profile == null) return Results.NotFound("Employee payroll profile not found.");

    var template = await dbContext.SalaryTemplates.FindAsync(profile.SalaryTemplateId);
    if (template == null) return Results.NotFound("Salary template not found.");

    var masterComponents = await dbContext.SalaryComponents.ToListAsync();
    var plan = CTCTemplateCompiler.Compile(template, masterComponents);
    var breakdown = CTCBreakdownService.Calculate(profile.AnnualCTC, plan);

    // 3. Build Ephemeral Declaration
    var fy = new FinancialYear(2026, 2027); // Standard for simulator
    var ephemeralDeclaration = ITDeclaration.Create(employeeId, fy);
    ephemeralDeclaration.Update(request.Section80C, request.Section80D, 0, request.MonthlyRent);

    // 4. Run Projections (Old vs New Regime)
    var ptProvider = sp.GetRequiredService<IProfessionalTaxProvider>();
    var projectionService = sp.GetRequiredService<IIncomeProjectionService>();
    var exemptionCalculator = sp.GetRequiredService<IExemptionCalculator>();
    var taxSlabProvider = sp.GetRequiredService<ITaxSlabProvider>();
    
    // Inject the ephemeral declaration via a static repository
    var staticRepo = new StaticDeclarationRepository(ephemeralDeclaration);
    
    // RuleSet with the static repo
    var ruleSet = new FY20262027RuleSet(
        new List<Guid> { Guid.Parse("00000000-0000-0000-0000-000000000001") }, // Basic ID
        ptProvider, projectionService, exemptionCalculator, taxSlabProvider, staticRepo);

    // Run Old Regime
    var oldProfile = profile.CloneWithRegime(TaxRegime.Old);
    var oldContext = new StatutoryContext(breakdown, oldProfile, fy, DateTime.UtcNow.Month);
    var oldResult = await StatutoryPipelineEngine.ExecuteAsync(oldContext, ruleSet);

    // Run New Regime
    var newProfile = profile.CloneWithRegime(TaxRegime.New);
    var newContext = new StatutoryContext(breakdown, newProfile, fy, DateTime.UtcNow.Month);
    var newResult = await StatutoryPipelineEngine.ExecuteAsync(newContext, ruleSet);

    decimal oldTax = oldResult.Deductions.GetValueOrDefault("TDS", 0);
    decimal newTax = newResult.Deductions.GetValueOrDefault("TDS", 0);

    return Results.Ok(new TaxSimulationResult(
        oldTax,
        newTax,
        Math.Abs(oldTax - newTax),
        oldTax < newTax ? "Old Regime" : "New Regime",
        oldTax < newTax ? "Based on your deductions, the Old Regime provides higher savings." : "The New Regime is more beneficial due to lower overall tax rates."
    ));
})
.RequireRateLimiting("ess");

// Uploads and analyzes a tax investment proof using Azure AI Document Intelligence.
app.MapPost("/api/ess/declarations/analyze", async (
    IFormFile file,
    IDocumentAnalyzer analyzer,
    IProofStorage storage) =>
{
    // 1. Security: Enforce strict file validation
    if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded.");
    if (file.Length > 5 * 1024 * 1024) return Results.BadRequest("File size exceeds 5MB limit.");
    
    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!allowedExtensions.Contains(extension)) return Results.BadRequest("Invalid file type. Only PDF, JPG, and PNG are supported.");

    // Context (Mocked for now)
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    var tenantId = "tenant_oakridge";
    var financialYear = 2026;

    using var stream = file.OpenReadStream();

    // 2. Store: Save to secure storage BEFORE analysis (Security & Compliance)
    // Path: /tax-proofs/{tenant}/{year}/{employeeId}/{guid}.pdf
    var storageUri = await storage.SaveAsync(stream, file.FileName, tenantId, employeeId, financialYear);

    // 3. Analyze: Run AI extraction (Security: Backend-only SDK call)
    stream.Position = 0; // Reset stream for analyzer
    var result = await analyzer.AnalyzeAsync(stream, file.FileName);

    // 4. Return: Return normalized DTO with Storage URI for audit linkage
    return Results.Ok(result with { StorageUri = storageUri });
})
.DisableAntiforgery()
.RequireRateLimiting("ai"); 

// --- Admin Tax Verification API ---

// Returns a queue of all pending tax declarations for HR review.
app.MapGet("/api/admin/declarations/pending", async (IITDeclarationRepository repo) =>
{
    var items = await repo.GetPendingReviewAsync();
    return Results.Ok(items.Select(x => new
    {
        x.Id,
        x.EmployeeId,
        x.Category,
        x.ClaimedAmount,
        x.SubmittedAt,
        x.ProofUri
    }));
});

// Streams a proof document to the HR split-view dashboard.
app.MapGet("/api/admin/declarations/{id}/document", async (Guid id, IITDeclarationRepository repo, IProofStorage storage) =>
{
    var declaration = await repo.GetByIdAsync(id);
    if (declaration == null) return Results.NotFound();

    var stream = await storage.GetStreamAsync(declaration.ProofUri);
    // Determine content type based on extension
    var extension = Path.GetExtension(declaration.ProofUri).ToLowerInvariant();
    var contentType = extension == ".pdf" ? "application/pdf" : "image/jpeg";
    
    return Results.File(stream, contentType);
});

// Approves a tax declaration with an explicit approved amount.
app.MapPut("/api/admin/declarations/{id}/approve", async (Guid id, [FromBody] decimal amount, ITDeclarationService service) =>
{
    // In a real app, get user from ClaimsPrincipal
    var approver = "HR_Admin_01";
    await service.ApproveAsync(id, amount, approver);
    return Results.NoContent();
});

// Rejects a tax declaration with a reason.
app.MapPut("/api/admin/declarations/{id}/reject", async (Guid id, [FromBody] string reason, ITDeclarationService service) =>
{
    var rejectedBy = "HR_Admin_01"; // In real app, resolve from JWT
    await service.RejectAsync(id, reason, rejectedBy);
    return Results.NoContent();
});

app.Run();

/// <summary>
/// Ephemeral repository for stateless tax simulations.
/// </summary>
public class StaticDeclarationRepository : IITDeclarationRepository
{
    private readonly ITDeclaration _declaration;
    public StaticDeclarationRepository(ITDeclaration declaration) => _declaration = declaration;
    
    public Task<IReadOnlyList<ITDeclaration>> GetApprovedDeclarationsAsync(Guid employeeId, int financialYear) 
        => Task.FromResult<IReadOnlyList<ITDeclaration>>(new List<ITDeclaration> { _declaration });

    public Task<ITDeclaration?> GetLatestAsync(Guid employeeId, int financialYear, string category) 
        => Task.FromResult<ITDeclaration?>(_declaration);

    public Task<List<ITDeclaration>> GetPendingReviewAsync() => Task.FromResult(new List<ITDeclaration>());
    public Task<ITDeclaration?> GetByIdAsync(Guid id) => Task.FromResult<ITDeclaration?>(null);
    public Task UpsertAsync(ITDeclaration declaration) => Task.CompletedTask;
    public Task SaveChangesAsync() => Task.CompletedTask;
}

namespace Karamchari.Api
{
    public record TaxSimulationRequest(decimal Section80C, decimal Section80D, decimal MonthlyRent, bool IsMetro, int FinancialYear);
    public record TaxSimulationResult(decimal OldRegimeTax, decimal NewRegimeTax, decimal Difference, string RecommendedRegime, string Reason);
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

    /// <summary>
    /// Request contract for creating a master salary component.
    /// </summary>
    public record CreateSalaryComponentRequest(string Name, ComponentType Type, RoundingStrategy Rounding);

    /// <summary>
    /// Request contract for creating a salary template.
    /// </summary>
    public record CreateSalaryTemplateRequest(string Name, List<SalaryTemplateComponent> Components);

    /// <summary>
    /// Request contract for calculating a CTC breakdown.
    /// </summary>
    public record CalculateCTCBreakdownRequest(decimal AnnualCTC, Dictionary<Guid, decimal>? Overrides);

    /// <summary>
    /// Request contract for a full statutory pay calculation.
    /// </summary>
    public record CalculateStatutoryRequest(
        decimal AnnualCTC, 
        Guid? EmployeeId, 
        List<Guid> EpfBaseComponentIds, 
        Dictionary<Guid, decimal>? Overrides);

    /// <summary>
    /// Request contract for locking a payroll run.
    /// </summary>
    /// <param name="ApprovedBy">The name of the approver.</param>
    public record LockPayrollRunRequest(string ApprovedBy);
}
