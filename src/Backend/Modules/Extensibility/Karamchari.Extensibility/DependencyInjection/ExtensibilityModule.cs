using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Extensibility.DependencyInjection;

public sealed class ExtensibilityModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;
    public ExtensibilityModule(IConfiguration configuration) => _configuration = configuration;
    public void RegisterServices(IServiceCollection services) => services.AddKaramchariExtensibility(_configuration);
    public void MapEndpoints(IEndpointRouteBuilder app) { }
    public void RegisterConsumers(IBusRegistrationConfigurator configurator) => configurator.AddKaramchariExtensibilityConsumers();
}
