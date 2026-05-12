using Karamchari.Billing.DependencyInjection;
using Karamchari.Capability.DependencyInjection;
using Karamchari.Compensation.DependencyInjection;
using Karamchari.Forecasting.DependencyInjection;
using Karamchari.Governance.DependencyInjection;
using Karamchari.HR.DependencyInjection;
using Karamchari.Identity.Infrastructure;
using Karamchari.Intelligence.DependencyInjection;
using Karamchari.Notifications.DependencyInjection;
using Karamchari.Payroll.DependencyInjection;
using Karamchari.Performance.DependencyInjection;
using Karamchari.PSA.DependencyInjection;
using Karamchari.Recruitment.DependencyInjection;
using Karamchari.TimeAttendance.DependencyInjection;
using Karamchari.Workflow.DependencyInjection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Karamchari.Worker.DependencyInjection;

/// <summary>
/// Provides extension methods for registering Worker-specific services.
/// </summary>
public static class WorkerServiceCollectionExtensions
{
    /// <summary>
    /// Configures MassTransit for the Worker role, registering all module consumers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The host environment.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddKaramchariWorkerMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddMassTransit(x =>
        {
            // Register All Module Consumers using unified extensions
            x.AddKaramchariHRConsumers();
            x.AddKaramchariWorkflowConsumers();
            x.AddKaramchariTimeAttendanceConsumers();
            x.AddKaramchariPayrollConsumers();
            x.AddKaramchariPerformanceConsumers();
            x.AddKaramchariNotificationsConsumers();
            x.AddKaramchariCompensationConsumers();
            x.AddKaramchariRecruitmentConsumers();
            x.AddKaramchariIntelligenceConsumers();
            x.AddKaramchariBillingConsumers();
            x.AddKaramchariForecastingConsumers();
            x.AddKaramchariPSAConsumers();

            // Register outboxes for all modules so workers can publish with transaction support
            // (Used by Sagas and Consumers that publish events)
            x.AddEntityFrameworkOutbox<Karamchari.HR.Persistence.HRDbContext>(o => o.UseSqlServer());
            x.AddEntityFrameworkOutbox<Karamchari.Payroll.Data.PayrollDbContext>(o => o.UseSqlServer());
            x.AddEntityFrameworkOutbox<Karamchari.TimeAttendance.Persistence.TimeAttendanceDbContext>(o => o.UseSqlServer());
            x.AddEntityFrameworkOutbox<Karamchari.PSA.Persistence.PSADbContext>(o => o.UseSqlServer());
            x.AddEntityFrameworkOutbox<Karamchari.Performance.Persistence.PerformanceDbContext>(o => o.UseSqlServer());
            x.AddEntityFrameworkOutbox<Karamchari.Notifications.Persistence.NotificationsDbContext>(o => o.UseSqlServer());
            x.AddEntityFrameworkOutbox<Karamchari.Compensation.Persistence.CompensationDbContext>(o => o.UseSqlServer());
            x.AddEntityFrameworkOutbox<Karamchari.Recruitment.Persistence.RecruitmentDbContext>(o => o.UseSqlServer());
            x.AddEntityFrameworkOutbox<Karamchari.Capability.Persistence.CapabilityDbContext>(o => o.UseSqlServer());
            x.AddEntityFrameworkOutbox<Karamchari.Intelligence.Persistence.IntelligenceDbContext>(o => o.UseSqlServer());
            x.AddEntityFrameworkOutbox<Karamchari.Governance.Persistence.GovernanceDbContext>(o => o.UseSqlServer());

            // Transport
            var rabbitMqConnection = configuration.GetConnectionString("RabbitMQ");
            if (!string.IsNullOrEmpty(rabbitMqConnection))
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqConnection);
                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        return services;
    }

    /// <summary>
    /// Consolidates all background services from different modules into the worker pool.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddKaramchariWorkerPool(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Consolidate all background workers here
        services.AddHostedService<Karamchari.Core.Middleware.IdempotencyCleanupWorker>();
        services.AddHostedService<Karamchari.Billing.Services.CollectionsBackgroundWorker>();
        services.AddHostedService<Karamchari.Intelligence.Services.DriftDetectionWorker>();
        services.AddHostedService<Karamchari.Notifications.BackgroundServices.DigestGenerationWorker>();

        // Identity Workers
        services.AddKaramchariIdentityWorkers();

        return services;
    }
}
