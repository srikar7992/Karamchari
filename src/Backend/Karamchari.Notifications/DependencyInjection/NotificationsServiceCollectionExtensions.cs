using Karamchari.Core.DependencyInjection;
using Karamchari.Notifications.Channels;
using Karamchari.Notifications.Consumers;
using Karamchari.Notifications.Orchestration;
using Karamchari.Notifications.Persistence;
using Karamchari.Notifications.RealTime;
using Karamchari.Notifications.Rendering;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Notifications.DependencyInjection;

public static class NotificationsServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariNotifications(
        this IServiceCollection services,
        IConfiguration configuration,
        IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(busConfigurator);

        // RLS tenant table registrations — security regression if any table is omitted.
        services.RegisterTenantTable("NotificationTemplates");
        services.RegisterTenantTable("UserNotificationPreferences");
        services.RegisterTenantTable("NotificationMessages");
        services.RegisterTenantTable("NotificationDeliveryAttempts");

        services.AddDbContext<NotificationsDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before NotificationsDbContext can be resolved.");

            options.UseSqlServer(connectionString);
            options.AddKaramchariInterceptors(serviceProvider);
        });

        // Orchestration pipeline services.
        services.AddScoped<ITemplateRenderer, TemplateRenderer>();
        services.AddScoped<INotificationOrchestrator, NotificationOrchestrator>();

        // Channel adapters — registered as INotificationChannelAdapter so the orchestrator
        // gets IEnumerable<INotificationChannelAdapter> and iterates them.
        services.AddScoped<INotificationChannelAdapter, InAppChannelAdapter>();
        services.AddScoped<INotificationChannelAdapter, EmailChannelAdapter>();

        // Real-time push service — HubNotificationPushService is registered in
        // Karamchari.Api (needs IHubContext<NotificationHub> from SignalR).
        // The interface is registered here; the concrete binding happens in Program.cs
        // after AddSignalR() is called.

        // MassTransit consumers.
        busConfigurator.AddConsumer<ReviewAssignedConsumer>();
        busConfigurator.AddConsumer<ReviewSubmittedConsumer>();
        busConfigurator.AddConsumer<CalibrationFinalizedConsumer>();
        busConfigurator.AddConsumer<PromotionApprovedConsumer>();
        busConfigurator.AddConsumer<GoalCycleActivatedConsumer>();
        busConfigurator.AddConsumer<FeedbackRequestCreatedConsumer>();
        busConfigurator.AddConsumer<GoalApprovalRequiredConsumer>();

        return services;
    }
}
