// -----------------------------------------------------------------------
// <copyright file="PlatformIntelligenceModule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.PlatformIntelligence.DependencyInjection;

public sealed class PlatformIntelligenceModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public PlatformIntelligenceModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariPlatformIntelligence(_configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app) { }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator) { }
}
