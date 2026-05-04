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
using Microsoft.EntityFrameworkCore;

// Entry point for the Karamchari API.
// Composes all bounded contexts and shared infrastructure.
var builder = WebApplication.CreateBuilder(args);

// Add Core Infrastructure (Multitenancy, Interceptors)
builder.Services.AddKaramchariCore(builder.Configuration);

// Add Bounded Contexts
builder.Services.AddKaramchariHR(builder.Configuration);
builder.Services.AddKaramchariTimeAttendance(builder.Configuration);

// Configure MassTransit with Transactional Outbox and Module Sagas
builder.Services.AddMassTransit(x =>
{
    // Transactional Outbox for HR Context
    x.AddEntityFrameworkOutbox<HRDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();
    });

    // Register Payroll Module (Sagas, Consumers, DB Context)
    // Note: The extension method also configures the Saga Repository in MassTransit.
    builder.Services.AddKaramchariPayroll(builder.Configuration, x);

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

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
}
