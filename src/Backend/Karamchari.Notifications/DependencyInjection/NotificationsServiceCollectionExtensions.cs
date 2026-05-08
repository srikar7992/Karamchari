using Karamchari.Core.DependencyInjection;
using Karamchari.Notifications.Persistence;
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
        MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(busConfigurator);

        // RLS tenant table registrations — security regression if any table is omitted
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

        return services;
    }
}
