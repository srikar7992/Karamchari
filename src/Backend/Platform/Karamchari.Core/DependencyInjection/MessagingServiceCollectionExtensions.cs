// -----------------------------------------------------------------------
// <copyright file="MessagingServiceCollectionExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Messaging;
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
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        var hasConsumers = configureConsumers is not null;

        services.AddMassTransit(x =>
        {
            // Allow callers (e.g. Worker host) to register module consumers via callback.
            configureConsumers?.Invoke(x);

            var isDev = environment.IsDevelopment();

            // Transport configuration
            var rabbitMqConnection = configuration.GetConnectionString("RabbitMQ");
            var serviceBusConnection = configuration.GetConnectionString("AzureServiceBus");

            // Fail fast: a non-Development host with no durable transport must not silently
            // fall through to (or omit) the in-memory transport (certification finding C-4).
            TransportConfigurationGuard.EnsureConfigured(isDev, rabbitMqConnection, serviceBusConnection);

            if (isDev && string.IsNullOrEmpty(rabbitMqConnection) && string.IsNullOrEmpty(serviceBusConnection))
            {
                x.UsingInMemory((context, cfg) =>
                {
                    if (hasConsumers) cfg.ConfigureEndpoints(context);
                    ConfigureFilters(cfg, context);
                });
            }
            else if (!string.IsNullOrEmpty(rabbitMqConnection))
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqConnection);
                    if (hasConsumers) cfg.ConfigureEndpoints(context);
                    ConfigureFilters(cfg, context);
                });
            }
            else if (!string.IsNullOrEmpty(serviceBusConnection))
            {
                x.UsingAzureServiceBus((context, cfg) =>
                {
                    cfg.Host(serviceBusConnection);
                    if (hasConsumers) cfg.ConfigureEndpoints(context);
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
