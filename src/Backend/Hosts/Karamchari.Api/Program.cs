using Karamchari.Api.DependencyInjection;
using Karamchari.Api.Middleware;
using Karamchari.Core.DependencyInjection;
using Karamchari.Identity.Infrastructure;
using Karamchari.Workflow.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 0. Fail-fast secret validation.
// Outside Development/Local, refuse to start with a missing, placeholder, or weak
// JWT signing secret. Without this guard a deployment that forgets to inject
// Jwt:Secret would SILENTLY sign tokens with the publicly-known committed
// placeholder, allowing trivial token forgery. Fail loud, not silent.
if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Local"))
{
    var jwtSecret = builder.Configuration["Jwt:Secret"];
    if (string.IsNullOrWhiteSpace(jwtSecret)
        || jwtSecret.StartsWith("REPLACE_VIA_ENV", StringComparison.OrdinalIgnoreCase)
        || jwtSecret.Contains("change_me", StringComparison.OrdinalIgnoreCase)
        || System.Text.Encoding.UTF8.GetByteCount(jwtSecret) < 32)
    {
        throw new InvalidOperationException(
            "Jwt:Secret is missing, a placeholder, or shorter than 32 bytes. " +
            "Supply a strong secret via environment variable 'Jwt__Secret', user-secrets, " +
            "or a secret store (e.g. Azure Key Vault) before starting in this environment.");
    }
}

// 1. Logging & Observability
builder.AddKaramchariLogging();

// 2. Identity & Security (Enterprise Foundation)
builder.Services.AddKaramchariIdentity(builder.Configuration);
builder.Services.AddAuthorization(options => options.AddKaramchariPermissionPolicies());

// 3. Infrastructure & Core
builder.Services.AddKaramchariCore(builder.Configuration);
builder.Services.AddOutboxRelay(builder.Configuration);
builder.Services.AddKaramchariInfrastructure(builder.Configuration);
builder.Services.AddKaramchariHealthChecks(builder.Configuration);
builder.Services.AddKaramchariResilience();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Karamchari API Explorer",
            Version = "v1",
            Description = "Enterprise-grade modular monolith workforce management platform API."
        };

        var securityScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Name = "Authorization",
            In = ParameterLocation.Header,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter JWT Bearer token."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = securityScheme;

        // NOTE: security is applied PER-OPERATION below (operation transformer), not globally.
        // A blanket document.Security would wrongly mark anonymous endpoints (login, register,
        // refresh, health) as requiring a Bearer token in Scalar.
        return Task.CompletedTask;
    });

    // Per-operation security: mark only endpoints that actually require authorization
    // (RequireAuthorization / [Authorize], and not [AllowAnonymous]) so Scalar shows the
    // correct lock icon per endpoint. Without this the OpenAPI doc gave no per-endpoint
    // auth signal — a developer could not tell which endpoints need a token from the doc.
    options.AddOperationTransformer((operation, context, _) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var requiresAuth =
            metadata.OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>().Any()
            && !metadata.OfType<Microsoft.AspNetCore.Authorization.IAllowAnonymous>().Any();

        if (requiresAuth)
        {
            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new()
                {
                    { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() }
                }
            };
        }

        return Task.CompletedTask;
    });
});

// 4. Messaging & Async Processing
builder.Services.AddKaramchariMassTransit(builder.Configuration, builder.Environment);

var app = builder.Build();

// 5. Request Pipeline
app.UseExceptionHandler(opt => { }); // Uses IExceptionHandler registered in services
app.UseKaramchariSecurityHeaders(); // OWASP security headers + correlation/trace response headers

if (app.Environment.IsDevelopment())
{
    // DeveloperExceptionPage leaks stack traces and request headers; restricted to
    // Development ONLY. Local and all other environments use the sanitized
    // GlobalExceptionHandler (RFC 7807, no internals).
    app.UseDeveloperExceptionPage();
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Karamchari API Explorer")
               .WithTheme(ScalarTheme.Purple);

        options.Authentication = new ScalarAuthenticationOptions
        {
            PreferredSecuritySchemes = new List<string> { "Bearer" }
        };
    });
    app.MapGet("/", () => Results.Redirect("/scalar"));
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseKaramchariTenantAuthorization();
app.UseAuthorization();
app.UseKaramchariTenantObservability();

// 6. Endpoints & Health
app.MapKaramchariHealthChecks();
app.MapKaramchariEndpoints();

if (args.Contains("--provision-dev-tenants"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {

        logger.LogInformation("Starting database migrations for all contexts...");

        var contextTypes = new Type[]
        {
        typeof(Karamchari.Identity.Infrastructure.Persistence.IdentityDbContext),
        typeof(Karamchari.HR.Persistence.HRDbContext),
        typeof(Karamchari.Payroll.Data.PayrollDbContext),
        typeof(Karamchari.TimeAttendance.Persistence.TimeAttendanceDbContext),
        typeof(Karamchari.PSA.Persistence.PSADbContext),
        typeof(Karamchari.Performance.Persistence.PerformanceDbContext),
        typeof(Karamchari.Notifications.Persistence.NotificationsDbContext),
        typeof(Karamchari.Compensation.Persistence.CompensationDbContext),
        typeof(Karamchari.Recruitment.Persistence.RecruitmentDbContext),
        typeof(Karamchari.DataMigration.Persistence.DataMigrationDbContext),
        typeof(Karamchari.Capability.Persistence.CapabilityDbContext),
        typeof(Karamchari.Intelligence.Persistence.IntelligenceDbContext),
        typeof(Karamchari.Governance.Persistence.GovernanceDbContext),
        typeof(Karamchari.Billing.Persistence.BillingDbContext),
        typeof(Karamchari.Forecasting.Persistence.ForecastingDbContext),
        typeof(Karamchari.Workflow.Persistence.WorkflowDbContext),
        typeof(Karamchari.FinancialOps.Persistence.FinancialOpsDbContext),
        typeof(Karamchari.Core.Messaging.Outbox.OutboxRelayDbContext),
        typeof(Karamchari.Onboarding.Persistence.OnboardingDbContext)
        };

        foreach (var type in contextTypes)
        {
            logger.LogInformation("Resolving {DbContext}...", type.Name);
            var db = (DbContext)scope.ServiceProvider.GetRequiredService(type);

            logger.LogInformation("Migrating {DbContext}...", type.Name);
            await db.Database.MigrateAsync();
            logger.LogInformation("Migrated {DbContext}.", type.Name);
        }

        // Seed the JWT signing key (idempotent) so token validation has a DB-backed key
        // that supports rotation. Falls back transparently to the configured Jwt:Secret.
        var identityDb = scope.ServiceProvider.GetRequiredService<Karamchari.Identity.Infrastructure.Persistence.IdentityDbContext>();
        if (!await identityDb.SigningKeys.AnyAsync())
        {
            var keyId = builder.Configuration["Jwt:ActiveKeyId"] ?? "primary";
            var secret = builder.Configuration["Jwt:Secret"];
            if (!string.IsNullOrWhiteSpace(secret))
            {
                identityDb.SigningKeys.Add(
                    Karamchari.Identity.Domain.SigningKeyMetadata.Create(keyId, secret, DateTimeOffset.UtcNow));
                await identityDb.SaveChangesAsync();
                logger.LogInformation("Seeded JWT signing key '{KeyId}'.", keyId);
            }
        }

        var provisioner = scope.ServiceProvider.GetRequiredService<Karamchari.Core.Persistence.Provisioning.TenantProvisioningService>();

        logger.LogInformation("Bootstrapping RLS infrastructure...");
        await provisioner.ProvisionRlsInfrastructureAsync();

        logger.LogInformation("Starting development tenant provisioning...");

        // Provision 'dev' tenant
        await provisioner.ProvisionTenantAsync("dev", "tenant_dev");

        // Add other test tenants
        await provisioner.ProvisionTenantAsync("acme", "tenant_acme");
        await provisioner.ProvisionTenantAsync("contoso", "tenant_contoso");
        await provisioner.ProvisionTenantAsync("globex", "tenant_globex");

        logger.LogInformation("Provisioning complete. Seeding developer users...");

        // Seed documented developer login users (admin@{tenant}.local, etc.) so a new
        // engineer can authenticate immediately without creating data by hand.
        await Karamchari.Api.Seeding.DevDataSeeder.SeedAsync(scope.ServiceProvider, logger);

        logger.LogInformation("Provisioning + developer seeding complete.");
        await Serilog.Log.CloseAndFlushAsync();
        // Force a deterministic, successful exit. Returning would dispose the host and
        // its background services, which previously aborted the process with exit 134.
        Environment.Exit(0);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Provisioning failed.");
        await Serilog.Log.CloseAndFlushAsync();
        Environment.Exit(1);
    }
}

app.Run();

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public partial class Program { }
