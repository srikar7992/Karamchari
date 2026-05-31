using Karamchari.Core.DependencyInjection;
using Karamchari.Notifications.BackgroundServices;
using Karamchari.Notifications.Channels;
using Karamchari.Notifications.Consumers;
using Karamchari.Notifications.Email;
using Karamchari.Notifications.Orchestration;
using Karamchari.Notifications.Persistence;
using Karamchari.Notifications.RealTime;
using Karamchari.Notifications.Rendering;
using Karamchari.Notifications.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Notifications.DependencyInjection;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class NotificationsServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static IServiceCollection AddKaramchariNotifications(
        this IServiceCollection services,
        IConfiguration configuration,
        IBusRegistrationConfigurator? busConfigurator = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (busConfigurator != null)
        {
            AddKaramchariNotificationsConsumers(busConfigurator);
        }

        // RLS tenant table registrations â€” security regression if any table is omitted.
        services.RegisterTenantTable("NotificationTemplates");
        services.RegisterTenantTable("UserNotificationPreferences");
        services.RegisterTenantTable("NotificationMessages");
        services.RegisterTenantTable("NotificationDeliveryAttempts");
        services.RegisterTenantTable("DigestBatches");

        services.AddDbContext<NotificationsDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before NotificationsDbContext can be resolved.");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
            options.AddKaramchariInterceptors(serviceProvider);
        });

        // Orchestration pipeline services.
        services.AddScoped<ITemplateRenderer, TemplateRenderer>();
        services.AddScoped<INotificationOrchestrator, NotificationOrchestrator>();

        // Channel adapters â€” registered as INotificationChannelAdapter so the orchestrator
        // gets IEnumerable<INotificationChannelAdapter> and iterates them.
        services.AddScoped<INotificationChannelAdapter, InAppChannelAdapter>();
        services.AddScoped<INotificationChannelAdapter, EmailChannelAdapter>();

        // Email provider â€” NullEmailProvider for Phase 1; swap to SendGrid/SES in Phase 2.
        services.AddScoped<IEmailProvider, NullEmailProvider>();

        // Digest engine.
        services.AddScoped<DigestAggregatorService>();

        return services;
    }

    /// <summary>
    /// Registers Notifications module consumers for MassTransit.
    /// </summary>
    public static void AddKaramchariNotificationsConsumers(this IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
        busConfigurator.AddConsumer<ReviewAssignedConsumer>();
        busConfigurator.AddConsumer<ReviewSubmittedConsumer>();
        busConfigurator.AddConsumer<CalibrationFinalizedConsumer>();
        busConfigurator.AddConsumer<PromotionApprovedConsumer>();
        busConfigurator.AddConsumer<GoalCycleActivatedConsumer>();
        busConfigurator.AddConsumer<FeedbackRequestCreatedConsumer>();
        busConfigurator.AddConsumer<GoalApprovalRequiredConsumer>();
        busConfigurator.AddConsumer<PayrollNotificationConsumer>();
    }
}
