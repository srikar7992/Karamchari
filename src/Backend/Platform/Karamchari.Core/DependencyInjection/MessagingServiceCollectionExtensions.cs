using Karamchari.Core.Messaging.Tenant;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Karamchari.Core.DependencyInjection;

/// <summary>
/// Centralized messaging configuration for the Karamchari platform.
/// Supports both API (Publish-only) and Worker (Consumer) topologies.
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddKaramchariMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        bool includeConsumers = true)
    {
        services.AddMassTransit(x =>
        {
            if (includeConsumers)
            {
                // Register consumers from all modules
                // Note: Modules must provide extension methods that accept IBusRegistrationConfigurator

                // Example registrations (should be called for each module)
                // services.AddKaramchariHR(configuration, x);
                // ...
            }

            var isDev = environment.IsDevelopment();

            // Transport configuration
            var rabbitMqConnection = configuration.GetConnectionString("RabbitMQ");
            var serviceBusConnection = configuration.GetConnectionString("AzureServiceBus");

            if (isDev && string.IsNullOrEmpty(rabbitMqConnection) && string.IsNullOrEmpty(serviceBusConnection))
            {
                x.UsingInMemory((context, cfg) =>
                {
                    if (includeConsumers) cfg.ConfigureEndpoints(context);
                    ConfigureFilters(cfg, context);
                });
            }
            else if (!string.IsNullOrEmpty(rabbitMqConnection))
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqConnection);
                    if (includeConsumers) cfg.ConfigureEndpoints(context);
                    ConfigureFilters(cfg, context);
                });
            }
            else if (!string.IsNullOrEmpty(serviceBusConnection))
            {
                x.UsingAzureServiceBus((context, cfg) =>
                {
                    cfg.Host(serviceBusConnection);
                    if (includeConsumers) cfg.ConfigureEndpoints(context);
                    ConfigureFilters(cfg, context);
                });
            }
        });

        return services;
    }

    private static void ConfigureFilters(IBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
        cfg.UsePublishFilter(typeof(TenantPublishFilter<>), context);
        cfg.UseSendFilter(typeof(TenantSendFilter<>), context);
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
    }
}
