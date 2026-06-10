// -----------------------------------------------------------------------
// <copyright file="PerformanceModule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Performance.DependencyInjection;

/// <summary>
/// Capability pack registration for the Performance module.
/// </summary>
public sealed class PerformanceModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public PerformanceModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariPerformance(_configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariPerformanceConsumers();
    }
}
