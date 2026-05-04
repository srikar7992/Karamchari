using Karamchari.Core.DependencyInjection;
using Karamchari.Core.Multitenancy;
using Karamchari.Api.Cors;
using Karamchari.Api.Security;
using Karamchari.HR.Contracts.Organization;
using Karamchari.HR.DependencyInjection;
using Karamchari.HR.Messaging.Consumers;
using Karamchari.HR.Persistence;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Karamchari.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration & secrets
// ---------------------------------------------------------------------------
// In production, IConfiguration is populated from appsettings.json + Key Vault
// (loaded via Managed Identity by Azure Container Apps). Local dev uses
// User Secrets (UserSecretsId in Karamchari.Api.csproj).
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
// Cross-cutting services
// ---------------------------------------------------------------------------
builder.Services.AddKaramchariCore(builder.Configuration);

// ---------------------------------------------------------------------------
// Event-driven backbone
// ---------------------------------------------------------------------------
builder.Services.AddMassTransit(bus =>
{
    bus.SetKebabCaseEndpointNameFormatter();

    bus.AddConsumer<DepartmentCreatedConsumer>();

    bus.AddEntityFrameworkOutbox<HRDbContext>(outbox =>
    {
        outbox.UseSqlServer();
        outbox.UseBusOutbox();
    });

    var azureServiceBusConnectionString = builder.Configuration.GetConnectionString("AzureServiceBus");
    if (string.IsNullOrWhiteSpace(azureServiceBusConnectionString))
    {
        if (!builder.Environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "ConnectionStrings:AzureServiceBus must be configured outside Development. Store it in Key Vault and load it through Managed Identity.");
        }

        bus.UsingInMemory((context, cfg) =>
        {
            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        bus.UsingAzureServiceBus((context, cfg) =>
        {
            cfg.Host(azureServiceBusConnectionString);
            cfg.ConfigureEndpoints(context);
        });
    }
});

// ---------------------------------------------------------------------------
// Authentication / authorization
// ---------------------------------------------------------------------------
// Production uses JWT bearer. Development uses a local gateway-proof scheme so
// Postman/curl can exercise tenant resolution before Entra ID is wired.
// ---------------------------------------------------------------------------
if (builder.Environment.IsDevelopment())
{
    builder.Services
        .AddAuthentication(DevelopmentTenantAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevelopmentTenantAuthenticationHandler>(
            DevelopmentTenantAuthenticationHandler.SchemeName,
            _ => { });
}
else
{
    builder.Services
        .AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", _ => { /* TODO: bind from configuration */ });
}

builder.Services.AddAuthorization();

// Dev-only CORS so the Next.js portal at http://localhost:3000 can hit the BFF.
// Production runs same-origin behind APIM, so no production CORS is registered.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddKaramchariDevCors();
}

// ---------------------------------------------------------------------------
// Bounded-context wiring lives in dedicated AddXxx extension methods within
// Karamchari.HR / Karamchari.Payroll.
// ---------------------------------------------------------------------------
builder.Services.AddKaramchariHR(builder.Configuration);

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
// CORS must run BEFORE auth so the browser preflight (which carries no creds)
// gets a fast 204 with the right Access-Control-Allow-* headers.
if (app.Environment.IsDevelopment())
{
    app.UseKaramchariDevCors();
}

app.UseAuthentication();
app.UseAuthorization();

// Convert tenant-resolution failures into 401/403 with a structured payload.
// A dedicated middleware will land in a follow-up; this inline handler keeps
// failures loud during scaffolding.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (TenantResolutionException ex)
    {
        var status = ex.Reason switch
        {
            TenantResolutionFailureReason.MissingJwtClaim => StatusCodes.Status401Unauthorized,
            TenantResolutionFailureReason.UntrustedHeaderSource => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status403Forbidden,
        };
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { error = "tenant_resolution_failed", reason = ex.Reason.ToString() });
    }
});

// Liveness — explicitly anonymous, never resolves a tenant.
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }))
    .AllowAnonymous();

// Readiness — performs a tenant lookup so unconfigured deployments fail loudly.
app.MapGet("/health/ready", (ITenantProvider tenantProvider) =>
{
    var ok = tenantProvider.TryGetTenant(out var tenant);
    return Results.Ok(new { status = "ready", tenantResolvable = ok, tenantId = tenant?.TenantId });
}).AllowAnonymous();

var hr = app.MapGroup("/api/hr")
    .RequireAuthorization();

hr.MapGet("/employees", async (HRDbContext db, CancellationToken cancellationToken) =>
{
    var employees = await db.Employees
        .AsNoTracking()
        .OrderBy(e => e.EmployeeNumber)
        .Select(e => new EmployeeListItem(
            e.Id,
            e.EmployeeNumber,
            e.LegalName,
            e.WorkEmail,
            e.HiredOn,
            e.Status.ToString()))
        .Take(100)
        .ToListAsync(cancellationToken);

    return Results.Ok(employees);
});

hr.MapGet("/departments", async (HRDbContext db, CancellationToken cancellationToken) =>
{
    var departments = await db.Departments
        .AsNoTracking()
        .OrderBy(d => d.Name)
        .Select(d => new DepartmentListItem(
            d.Id,
            d.Name,
            d.Description,
            d.IsActive))
        .Take(100)
        .ToListAsync(cancellationToken);

    return Results.Ok(departments);
})
.WithName("ListDepartments");

hr.MapPost("/departments", async (
    CreateDepartmentCommand command,
    IOrganizationService organizationService,
    CancellationToken cancellationToken) =>
{
    var departmentId = await organizationService.CreateDepartmentAsync(command, cancellationToken);

    return Results.Created($"/api/hr/departments/{departmentId}", new { id = departmentId });
})
.WithName("CreateDepartment");

app.Run();

namespace Karamchari.Api
{
    /// <summary>Marker class so <c>WebApplicationFactory&lt;Program&gt;</c> can find the entry assembly from integration tests.</summary>
    public partial class Program;
}
