// -----------------------------------------------------------------------
// <copyright file="CapabilityModule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Capability.Services;
using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Capability.DependencyInjection;

public sealed class CapabilityModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public CapabilityModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariCapability(_configuration);
        services.AddHostedService<ComplianceTrainingScheduler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariCapabilityConsumers();
    }
}
