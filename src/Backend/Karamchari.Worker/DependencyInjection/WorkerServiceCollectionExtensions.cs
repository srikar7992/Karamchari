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
/// Prepared for Step 13 partitioning: logical grouping of consumers.
/// </summary>
public static class WorkerServiceCollectionExtensions
{
    public static IServiceCollection AddKaramchariWorkerMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddMassTransit(x =>
        {
            // 1. High Priority (Payroll & HR)
            RegisterHighPriorityConsumers(x);

            // 2. Medium Priority (Workflow & Approvals)
            RegisterWorkflowConsumers(x);

            // 3. Analytics & Forecasting (Low Priority/Heavy)
            RegisterAnalyticsConsumers(x, configuration);

            // 4. Notifications & Capability
            RegisterNotificationConsumers(x);

            // Outboxes
            RegisterOutboxes(x);

            // Transport
            var rabbitMqConnection = configuration.GetConnectionString("RabbitMQ");
            if (!string.IsNullOrEmpty(rabbitMqConnection))
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqConnection);
                    
                    // Future: Split into different receive endpoints per priority
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

    private static void RegisterHighPriorityConsumers(IBusRegistrationConfigurator x)
    {
        // HR & Payroll are critical for business continuity
        x.AddKaramchariHRConsumers();
        x.AddKaramchariPayrollConsumers();
        x.AddKaramchariTimeAttendanceConsumers();
        x.AddKaramchariRecruitmentConsumers();
        x.AddKaramchariCompensationConsumers();
    }

    private static void RegisterWorkflowConsumers(IBusRegistrationConfigurator x)
    {
        // Coordination logic
        x.AddKaramchariWorkflowConsumers();
    }

    private static void RegisterAnalyticsConsumers(IBusRegistrationConfigurator x, IConfiguration cfg)
    {
        // Heavy processing
        x.AddKaramchariIntelligenceConsumers();
        x.AddKaramchariBillingConsumers();
        x.AddKaramchariForecastingConsumers();
        x.AddKaramchariPSAConsumers();
    }

    private static void RegisterNotificationConsumers(IBusRegistrationConfigurator x)
    {
        // Outbound communication
        x.AddKaramchariNotificationsConsumers();
        x.AddKaramchariCapabilityConsumers();
    }

    private static void RegisterOutboxes(IBusRegistrationConfigurator x)
    {
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
        x.AddEntityFrameworkOutbox<Karamchari.Billing.Persistence.BillingDbContext>(o => o.UseSqlServer());
        x.AddEntityFrameworkOutbox<Karamchari.Forecasting.Persistence.ForecastingDbContext>(o => o.UseSqlServer());
        x.AddEntityFrameworkOutbox<Karamchari.Workflow.Persistence.WorkflowDbContext>(o => o.UseSqlServer());
    }

    /// <summary>
    /// Consolidates all background services from different modules into the worker pool.
    /// </summary>
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
