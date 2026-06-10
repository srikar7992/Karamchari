// -----------------------------------------------------------------------
// <copyright file="CapabilityModule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Capability.DependencyInjection;

/// <summary>
/// Capability pack registration for the Capability module.
/// </summary>
public sealed class CapabilityModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CapabilityModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariCapability(_configuration);
    }

    /// <inheritdoc/>
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }

    /// <inheritdoc/>
    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariCapabilityConsumers();
    }
}
