using Karamchari.Api.DependencyInjection;
using Karamchari.Core.DependencyInjection;
using Karamchari.Identity.Infrastructure;
using Karamchari.Workflow.DependencyInjection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Logging & Observability
builder.AddKaramchariLogging();

// 2. Identity & Security (Enterprise Foundation)
builder.Services.AddKaramchariIdentity(builder.Configuration);
builder.Services.AddAuthorization();

// 3. Infrastructure & Core
builder.Services.AddKaramchariCore(builder.Configuration);
builder.Services.AddKaramchariInfrastructure(builder.Configuration);
builder.Services.AddKaramchariHealthChecks(builder.Configuration);
builder.Services.AddKaramchariResilience();

// 4. Messaging & Async Processing
builder.Services.AddKaramchariMassTransit(builder.Configuration, builder.Environment);

var app = builder.Build();

// 5. Request Pipeline
app.UseExceptionHandler(opt => { }); // Uses IExceptionHandler registered in services

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseKaramchariTenantObservability();

// 6. Endpoints & Health
app.MapKaramchariHealthChecks();
app.MapKaramchariEndpoints();

if (args.Contains("--provision-dev-tenants"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Starting database migrations for all contexts...");

    var dbContexts = new Microsoft.EntityFrameworkCore.DbContext[]
    {
        scope.ServiceProvider.GetRequiredService<Karamchari.HR.Persistence.HRDbContext>(),
        scope.ServiceProvider.GetRequiredService<Karamchari.Payroll.Data.PayrollDbContext>(),
        scope.ServiceProvider.GetRequiredService<Karamchari.TimeAttendance.Persistence.TimeAttendanceDbContext>(),
        scope.ServiceProvider.GetRequiredService<Karamchari.PSA.Persistence.PSADbContext>(),
        scope.ServiceProvider.GetRequiredService<Karamchari.Performance.Persistence.PerformanceDbContext>(),
        scope.ServiceProvider.GetRequiredService<Karamchari.Notifications.Persistence.NotificationsDbContext>(),
        scope.ServiceProvider.GetRequiredService<Karamchari.Compensation.Persistence.CompensationDbContext>(),
        scope.ServiceProvider.GetRequiredService<Karamchari.Recruitment.Persistence.RecruitmentDbContext>(),
        scope.ServiceProvider.GetRequiredService<Karamchari.Capability.Persistence.CapabilityDbContext>(),
        scope.ServiceProvider.GetRequiredService<Karamchari.Intelligence.Persistence.IntelligenceDbContext>(),
        scope.ServiceProvider.GetRequiredService<Karamchari.Governance.Persistence.GovernanceDbContext>(),
        scope.ServiceProvider.GetRequiredService<Karamchari.Billing.Persistence.BillingDbContext>(),
        scope.ServiceProvider.GetRequiredService<Karamchari.Forecasting.Persistence.ForecastingDbContext>(),
        scope.ServiceProvider.GetRequiredService<Karamchari.Core.Messaging.Outbox.OutboxRelayDbContext>()
    };

    foreach (var db in dbContexts)
    {
        logger.LogInformation("Migrating {DbContext}...", db.GetType().Name);
        await db.Database.MigrateAsync();
    }

    var provisioner = scope.ServiceProvider.GetRequiredService<Karamchari.Core.Persistence.Provisioning.TenantProvisioningService>();

    logger.LogInformation("Starting development tenant provisioning...");

    // Provision 'dev' tenant
    await provisioner.ProvisionTenantAsync("dev", "tenant_dev");

    // Add other test tenants
    await provisioner.ProvisionTenantAsync("acme", "tenant_acme");
    await provisioner.ProvisionTenantAsync("contoso", "tenant_contoso");

    logger.LogInformation("Provisioning complete.");
    return;
}

app.Run();

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public partial class Program { }
