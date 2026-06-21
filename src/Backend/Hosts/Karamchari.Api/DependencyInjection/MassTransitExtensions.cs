// -----------------------------------------------------------------------
// <copyright file="MassTransitExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Billing.Persistence;
using Karamchari.Capability.Persistence;
using Karamchari.Compensation.Persistence;
using Karamchari.Core.Messaging;
using Karamchari.Core.Messaging.Tenant;
using Karamchari.Core.Multitenancy.Execution;
using Karamchari.Core.Observability;
using Karamchari.DataMigration.Persistence;
using Karamchari.FinancialOps.Persistence;
using Karamchari.Forecasting.Persistence;
using Karamchari.Governance.Persistence;
using Karamchari.HR.Persistence;
using Karamchari.Intelligence.Persistence;
using Karamchari.Notifications.Persistence;
using Karamchari.Onboarding.Persistence;
using Karamchari.Payroll.Data;
using Karamchari.Performance.Persistence;
using Karamchari.PSA.Persistence;
using Karamchari.Recruitment.Persistence;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.Workflow.Persistence;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Karamchari.Api.DependencyInjection;

/// <summary>
/// MassTransit service collection extensions for the API.
/// Registers the bus, transport (RabbitMQ/InMemory), transactional EF
/// persistence outboxes, and cross-cutting tenant observability filters.
/// </summary>
public static class MassTransitExtensions
{
    /// <summary>
    /// Configures MassTransit for the API, enabling transactional outbox for all modules.
    /// </summary>
    public static IServiceCollection AddKaramchariMassTransit(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // API Needs module services but NOT consumers
        var modules = CapabilityRegistry.GetModules(configuration);
        foreach (var module in modules)
        {
            module.RegisterServices(services);
        }

        services.AddSingleton(typeof(TenantPublishFilter<>));
        services.AddSingleton(typeof(TenantSendFilter<>));

        // Event telemetry — answers "how many events fired? consumer latency? DLQ rate?"
        services.AddSingleton<KaramchariEventMetrics>();
        services.AddSingleton<KaramchariPublishObserver>();
        services.AddSingleton<KaramchariConsumeObserver>();

        services.AddMassTransit(x =>
        {
            // API only publishes, it doesn't need to register consumers
            // except for those needed for synchronous request/response or 
            // internal API-only async flows (rare in this topology).

            x.AddPublishObserver<KaramchariPublishObserver>();
            x.AddConsumeObserver<KaramchariConsumeObserver>();

            var isDev = environment.IsDevelopment();

            // Fail fast: a non-Development host with no durable transport must not silently
            // fall through to the in-memory transport (certification finding C-4).
            TransportConfigurationGuard.EnsureConfigured(
                isDev,
                configuration.GetConnectionString("RabbitMQ"),
                configuration.GetConnectionString("AzureServiceBus"));

            // Register outboxes for all modules so API can publish with transaction support
            x.AddEntityFrameworkOutbox<HRDbContext>(o =>
            {
                o.UseSqlServer();
                o.UseBusOutbox();
            });
            x.AddEntityFrameworkOutbox<FinancialOpsDbContext>(o =>
            {
                o.UseSqlServer();
            });
            x.AddEntityFrameworkOutbox<PayrollDbContext>(o =>
            {
                o.UseSqlServer();
            });
            x.AddEntityFrameworkOutbox<TimeAttendanceDbContext>(o =>
            {
                o.UseSqlServer();
            });
            x.AddEntityFrameworkOutbox<PSADbContext>(o =>
            {
                o.UseSqlServer();
            });
            x.AddEntityFrameworkOutbox<PerformanceDbContext>(o =>
            {
                o.UseSqlServer();
            });
            x.AddEntityFrameworkOutbox<NotificationsDbContext>(o =>
            {
                o.UseSqlServer();
            });
            x.AddEntityFrameworkOutbox<CompensationDbContext>(o =>
            {
                o.UseSqlServer();
            });
            x.AddEntityFrameworkOutbox<CapabilityDbContext>(o =>
            {
                o.UseSqlServer();
            });
            x.AddEntityFrameworkOutbox<DataMigrationDbContext>(o =>
            {
                o.UseSqlServer();
            });
            x.AddEntityFrameworkOutbox<RecruitmentDbContext>(o =>
            {
                o.UseSqlServer();
            });
            x.AddEntityFrameworkOutbox<IntelligenceDbContext>(o =>
            {
                o.UseSqlServer();
            });
            x.AddEntityFrameworkOutbox<GovernanceDbContext>(o =>
            {
                o.UseSqlServer();
            });
            x.AddEntityFrameworkOutbox<BillingDbContext>(o =>
            {
                o.UseSqlServer();
            });
            x.AddEntityFrameworkOutbox<ForecastingDbContext>(o =>
            {
                o.UseSqlServer();
            });
            x.AddEntityFrameworkOutbox<WorkflowDbContext>(o =>
            {
                o.UseSqlServer();
            });
            x.AddEntityFrameworkOutbox<OnboardingDbContext>(o =>
            {
                o.UseSqlServer();
            });

            if (isDev && string.IsNullOrEmpty(configuration.GetConnectionString("RabbitMQ")))
            {
                x.UsingInMemory((context, cfg) =>
                {
                    // No ConfigureEndpoints(context) here for API
                    cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
                    cfg.UsePublishFilter(typeof(TenantPublishFilter<>), context);
                    cfg.UsePublishFilter<TenantPublishFilter>(context);
                    cfg.UseSendFilter(typeof(TenantSendFilter<>), context);
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

                        // Tenant isolation MUST be enforced on the RabbitMQ transport with the
                        // same filters as the InMemory and Azure Service Bus branches. Omitting
                        // these previously meant messages published/consumed over RabbitMQ carried
                        // no enforced tenant context (cross-tenant leakage risk).
                        cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
                        cfg.UsePublishFilter(typeof(TenantPublishFilter<>), context);
                        cfg.UsePublishFilter<TenantPublishFilter>(context);
                        cfg.UseSendFilter(typeof(TenantSendFilter<>), context);
                        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

                        // Declare topology / receive endpoints for any registered consumers.
                        cfg.ConfigureEndpoints(context);
                    });
                }
                else
                {
                    var asbConnection = configuration.GetConnectionString("AzureServiceBus");
                    if (!string.IsNullOrEmpty(asbConnection))
                    {
                        x.UsingAzureServiceBus((context, cfg) =>
                        {
                            cfg.Host(asbConnection);
                            cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
                            cfg.UsePublishFilter(typeof(TenantPublishFilter<>), context);
                            cfg.UsePublishFilter<TenantPublishFilter>(context);
                            cfg.UseSendFilter(typeof(TenantSendFilter<>), context);
                            cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                        });
                    }
                    else
                    {
                        // Unreachable: TransportConfigurationGuard.EnsureConfigured already threw
                        // for a non-Development host with no broker. Kept as a defensive invariant
                        // so the in-memory transport can never be selected outside Development (C-4).
                        throw new InvalidOperationException(
                            "No production message transport configured. A non-Development host requires a "
                            + "'RabbitMQ' or 'AzureServiceBus' connection string (certification finding C-4).");
                    }
                }
            }
        });

        return services;
    }
}
