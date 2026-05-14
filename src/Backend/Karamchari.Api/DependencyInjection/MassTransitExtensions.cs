using Karamchari.Billing.DependencyInjection;
using Karamchari.Capability.DependencyInjection;
using Karamchari.Capability.Persistence;
using Karamchari.Compensation.DependencyInjection;
using Karamchari.Compensation.Persistence;
using Karamchari.Core.DependencyInjection;
using Karamchari.Core.Messaging.Outbox;
using Karamchari.Core.Messaging.Tenant;
using Karamchari.FinancialOps.Persistence;
using Karamchari.Forecasting.DependencyInjection;
using Karamchari.Governance.DependencyInjection;
using Karamchari.Governance.Persistence;
using Karamchari.HR.DependencyInjection;
using Karamchari.HR.Persistence;
using Karamchari.Intelligence.DependencyInjection;
using Karamchari.Intelligence.Persistence;
using Karamchari.Notifications.DependencyInjection;
using Karamchari.Notifications.Persistence;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.DependencyInjection;
using Karamchari.Performance.DependencyInjection;
using Karamchari.Performance.Persistence;
using Karamchari.PSA.Persistence;
using Karamchari.Recruitment.DependencyInjection;
using Karamchari.Recruitment.Persistence;
using Karamchari.TimeAttendance.DependencyInjection;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.Workflow.DependencyInjection;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.DependencyInjection;

/// <summary>
/// Contains extension methods for configuring MassTransit with bounded contexts,
/// outbox support, and tenant-aware messaging filters.
/// </summary>
public static class MassTransitExtensions
{
    /// <summary>
    /// Configures MassTransit for the platform, registering bounded context consumers,
    /// persistence outboxes, and cross-cutting tenant observability filters.
    /// </summary>
    public static IServiceCollection AddKaramchariMassTransit(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // API Needs module services but NOT consumers
        var modules = CapabilityRegistry.GetModules(configuration);
        foreach (var module in modules)
        {
            module.RegisterServices(services);
        }

        services.AddMassTransit(x =>
        {
            // API only publishes, it doesn't need to register consumers
            // except for those needed for synchronous request/response or 
            // internal API-only async flows (rare in this topology).

            var isDev = environment.IsDevelopment();

            // Register outboxes for all modules so API can publish with transaction support
            x.AddEntityFrameworkOutbox<HRDbContext>(o =>
            {
                o.UseSqlServer();
                if (isDev) o.UseBusOutbox();
            });
            x.AddEntityFrameworkOutbox<FinancialOpsDbContext>(o =>
            {
                o.UseSqlServer();
                if (isDev) o.UseBusOutbox();
            });
            x.AddEntityFrameworkOutbox<PayrollDbContext>(o =>
            {
                o.UseSqlServer();
                if (isDev) o.UseBusOutbox();
            });
            x.AddEntityFrameworkOutbox<TimeAttendanceDbContext>(o =>
            {
                o.UseSqlServer();
                if (isDev) o.UseBusOutbox();
            });
            x.AddEntityFrameworkOutbox<PSADbContext>(o =>
            {
                o.UseSqlServer();
                if (isDev) o.UseBusOutbox();
            });
            x.AddEntityFrameworkOutbox<PerformanceDbContext>(o =>
            {
                o.UseSqlServer();
                if (isDev) o.UseBusOutbox();
            });
            x.AddEntityFrameworkOutbox<NotificationsDbContext>(o =>
            {
                o.UseSqlServer();
                if (isDev) o.UseBusOutbox();
            });
            x.AddEntityFrameworkOutbox<CompensationDbContext>(o =>
            {
                o.UseSqlServer();
                if (isDev) o.UseBusOutbox();
            });
            x.AddEntityFrameworkOutbox<RecruitmentDbContext>(o =>
            {
                o.UseSqlServer();
                if (isDev) o.UseBusOutbox();
            });
            x.AddEntityFrameworkOutbox<CapabilityDbContext>(o =>
            {
                o.UseSqlServer();
                if (isDev) o.UseBusOutbox();
            });
            x.AddEntityFrameworkOutbox<IntelligenceDbContext>(o =>
            {
                o.UseSqlServer();
                if (isDev) o.UseBusOutbox();
            });
            x.AddEntityFrameworkOutbox<GovernanceDbContext>(o =>
            {
                o.UseSqlServer();
                if (isDev) o.UseBusOutbox();
            });

            if (isDev && string.IsNullOrEmpty(configuration.GetConnectionString("RabbitMQ")))
            {
                x.UsingInMemory((context, cfg) =>
                {
                    // No ConfigureEndpoints(context) here for API
                    cfg.UseConsumeFilter<TenantConsumeFilter>(context);
                    cfg.UsePublishFilter<TenantPublishFilter>(context);
                    cfg.UseSendFilter<TenantSendFilter>(context);
                    cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                });
            }
            else
            {
                var rabbitMqConnection = configuration.GetConnectionString("RabbitMQ");
                if (!string.IsNullOrEmpty(rabbitMqConnection))
                {
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.Host(rabbitMqConnection);
                        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                    });
                }
                else
                {
                    x.UsingAzureServiceBus((context, cfg) =>
                    {
                        cfg.Host(configuration.GetConnectionString("AzureServiceBus"));
                        cfg.UseConsumeFilter<TenantConsumeFilter>(context);
                        cfg.UsePublishFilter<TenantPublishFilter>(context);
                        cfg.UseSendFilter<TenantSendFilter>(context);
                        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                    });
                }
            }
        });

        return services;
    }
}
