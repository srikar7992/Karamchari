using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Workflow.DependencyInjection;

/// <summary>
/// Capability pack registration for the Workflow module.
/// </summary>
public sealed class WorkflowModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public WorkflowModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariWorkflow(_configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // Workflow APIs
    }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariWorkflowConsumers();
    }
}
