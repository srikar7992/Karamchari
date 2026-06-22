using Karamchari.Extensibility.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Extensibility.DependencyInjection;

public static class ExtensibilityServiceCollectionExtensions
{
    public static IServiceCollection AddKaramchariExtensibility(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ExtensibilityDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));
        return services;
    }

    public static void AddKaramchariExtensibilityConsumers(this IBusRegistrationConfigurator configurator) { }
}
