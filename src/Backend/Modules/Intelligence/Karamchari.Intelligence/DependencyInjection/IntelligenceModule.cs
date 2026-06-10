// -----------------------------------------------------------------------
// <copyright file="IntelligenceModule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Intelligence.DependencyInjection;

/// <summary>
/// Capability pack registration for the Intelligence module.
/// </summary>
public sealed class IntelligenceModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IntelligenceModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariIntelligence(_configuration);
    }

    /// <inheritdoc/>
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }

    /// <inheritdoc/>
    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariIntelligenceConsumers();
    }
}
