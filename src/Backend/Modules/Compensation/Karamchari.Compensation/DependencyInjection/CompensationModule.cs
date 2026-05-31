using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Compensation.DependencyInjection;

/// <summary>
/// Capability pack registration for the Compensation module.
/// </summary>
public sealed class CompensationModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public CompensationModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariCompensation(_configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariCompensationConsumers();
    }
}
