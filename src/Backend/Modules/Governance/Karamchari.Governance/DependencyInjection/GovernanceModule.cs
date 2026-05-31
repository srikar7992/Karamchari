using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Governance.DependencyInjection;

/// <summary>
/// Capability pack registration for the Governance module.
/// </summary>
public sealed class GovernanceModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public GovernanceModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariGovernance(_configuration);
    }

    /// <inheritdoc/>
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }

    /// <inheritdoc/>
    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariGovernanceConsumers();
    }
}
