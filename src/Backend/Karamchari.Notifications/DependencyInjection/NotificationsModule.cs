using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Notifications.DependencyInjection;

/// <summary>
/// Capability pack registration for the Notifications module.
/// </summary>
public sealed class NotificationsModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public NotificationsModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariNotifications(_configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariNotificationsConsumers();
    }
}
