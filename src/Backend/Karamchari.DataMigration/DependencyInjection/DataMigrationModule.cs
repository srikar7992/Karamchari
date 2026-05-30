using Karamchari.Core.DependencyInjection;
using Karamchari.DataMigration.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.DataMigration.DependencyInjection;

/// <summary>
/// Capability pack registration for the Data Migration module.
/// </summary>
public sealed class DataMigrationModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public DataMigrationModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariDataMigration(_configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        API.MigrationEndpoints.MapMigrationEndpoints(app);
    }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<Consumers.ImportJobQueuedConsumer>();
    }
}
