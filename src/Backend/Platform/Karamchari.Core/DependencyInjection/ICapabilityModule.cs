using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Core.DependencyInjection;

/// <summary>
/// Defines the contract for a commercial capability pack.
/// Enforces isolated registration of services, endpoints, and consumers.
/// Prevents the API project from becoming a "God Process" by delegating
/// initialization to the capability boundaries.
/// </summary>
public interface ICapabilityModule
{
    /// <summary>
    /// Registers domain services, repositories, and persistence for this capability.
    /// </summary>
    void RegisterServices(IServiceCollection services);

    /// <summary>
    /// Maps HTTP endpoints (BFF) for this capability.
    /// </summary>
    void MapEndpoints(IEndpointRouteBuilder app);

    /// <summary>
    /// Registers async messaging consumers for this capability.
    /// </summary>
    void RegisterConsumers(IBusRegistrationConfigurator configurator);
}
