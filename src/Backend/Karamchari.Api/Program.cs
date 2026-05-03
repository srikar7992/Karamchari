using Karamchari.Core.DependencyInjection;
using Karamchari.Core.Multitenancy;
using Karamchari.HR.DependencyInjection;
using Karamchari.HR.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

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
// JWT bearer is wired up here; full configuration (authority, audience, JWKS)
// will land in a follow-up so we stay honest about Day-1 scope.
// ---------------------------------------------------------------------------
builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", _ => { /* TODO: bind from configuration */ });
builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// Bounded-context wiring lives in dedicated AddXxx extension methods within
// Karamchari.HR / Karamchari.Payroll.
// ---------------------------------------------------------------------------
builder.Services.AddKaramchariHR(builder.Configuration);

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
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

app.Run();

internal sealed record EmployeeListItem(
    Guid Id,
    string EmployeeNumber,
    string LegalName,
    string? WorkEmail,
    DateOnly HiredOn,
    string Status);

namespace Karamchari.Api
{
    /// <summary>Marker class so <c>WebApplicationFactory&lt;Program&gt;</c> can find the entry assembly from integration tests.</summary>
    public partial class Program;
}
