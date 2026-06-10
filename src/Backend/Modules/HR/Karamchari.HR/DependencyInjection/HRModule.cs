// -----------------------------------------------------------------------
// <copyright file="HRModule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.HR.DependencyInjection;

/// <summary>
/// Capability pack registration for the HR module.
/// </summary>
public sealed class HRModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public HRModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariHR(_configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // Add HR endpoints here if they move out of Api.BFF.
    }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariHRConsumers();
    }
}
