using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Succession.DependencyInjection;

public sealed class SuccessionModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public SuccessionModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariSuccession(_configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app) { }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.AddConsumer<Karamchari.Succession.Consumers.PositionCreatedConsumer>();
        configurator.AddConsumer<Karamchari.Succession.Consumers.PositionAssignmentCreatedConsumer>();
        configurator.AddConsumer<Karamchari.Succession.Consumers.PositionAssignmentEndedConsumer>();
        configurator.AddConsumer<Karamchari.Succession.Consumers.SuccessorAddedConsumer>();
        configurator.AddConsumer<Karamchari.Succession.Consumers.SuccessorPromotedConsumer>();
    }
}
