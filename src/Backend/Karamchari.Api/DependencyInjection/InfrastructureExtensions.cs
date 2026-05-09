using FluentValidation;
using Karamchari.HR.Persistence;
using Karamchari.Notifications.RealTime;
using Karamchari.PSA.Hubs;
using Karamchari.PSA.Persistence;
using Karamchari.PSA.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Threading.RateLimiting;

namespace Karamchari.Api.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddKaramchariInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Exception Handling
        services.AddExceptionHandler<Karamchari.Api.Middleware.GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Validation
        services.AddValidatorsFromAssemblyContaining<Karamchari.Api.Validation.StartPayrollRunRequestValidator>();

        // Rate Limiting Configuration
        services.AddRateLimiter(options =>
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

        // OpenTelemetry
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(o =>
                {
                    o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                    o.EnrichWithHttpRequest = (activity, request) =>
                    {
                        var tenantId = request.HttpContext.User.FindFirst("tenant_id")?.Value;
                        if (!string.IsNullOrEmpty(tenantId))
                        {
                            activity.SetTag("tenant.id", tenantId);
                        }
                    };
                })
                .AddEntityFrameworkCoreInstrumentation(o =>
                {
                    o.SetDbStatementForText = true;
                })
                .AddSource("MassTransit")
                .AddSource("Karamchari.*"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation());

        // PSA context services (moved from Program.cs)
        services.AddDbContext<PSADbContext>(o =>
            o.UseSqlServer(configuration.GetConnectionString("KaramchariDb")));
        services.AddScoped<ProjectResourceRepository>();
        services.AddScoped<InvoiceGeneratorService>();
        services.AddScoped<EmployeeCostService>();
        services.AddSingleton<PricingEngine>();
        services.AddSingleton<SimulationService>();
        services.AddSingleton<AnomalyDetectionService>();
        services.AddScoped<CashFlowService>();
        services.AddSingleton<AnalyticsBroadcaster>();

        // Compliance Generators
        services.AddScoped<Karamchari.Payroll.Services.Compliance.IEcrGenerator, Karamchari.Payroll.Services.Compliance.EcrGenerator>();
        services.AddScoped<Karamchari.Payroll.Services.Compliance.IEsicGenerator, Karamchari.Payroll.Services.Compliance.EsicGenerator>();
        services.AddScoped<Karamchari.Payroll.Services.Compliance.ITdsGenerator, Karamchari.Payroll.Services.Compliance.TdsGenerator>();
        services.AddScoped<Karamchari.Payroll.Services.Compliance.IComplianceRiskEngine, Karamchari.Payroll.Services.Compliance.ComplianceRiskEngine>();
        services.AddScoped<Karamchari.Payroll.Services.Compliance.IComplianceService, Karamchari.Payroll.Services.Compliance.ComplianceService>();

        // SignalR
        services.AddSignalR();
        services.AddScoped<INotificationPushService, HubNotificationPushService>();

        // Provisioning Service Registration (Standardized)
        services.AddScoped<Karamchari.Core.Persistence.Provisioning.TenantProvisioningService>(sp =>
            new Karamchari.Core.Persistence.Provisioning.TenantProvisioningService(
                sp.GetRequiredService<HRDbContext>(),
                sp.GetRequiredService<Karamchari.Core.Persistence.Provisioning.ITenantTableRegistry>(),
                sp.GetRequiredService<Karamchari.Core.Persistence.Provisioning.RlsScriptGenerator>(),
                sp.GetRequiredService<IEnumerable<Karamchari.Core.Persistence.Provisioning.ITenantPostProvisioningTask>>(),
                sp.GetRequiredService<ILogger<Karamchari.Core.Persistence.Provisioning.TenantProvisioningService>>()));

        // Idempotency Cleanup
        services.AddHostedService<Karamchari.Api.Middleware.BackgroundServices.IdempotencyCleanupWorker>();

        return services;
    }
}
