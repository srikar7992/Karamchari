// -----------------------------------------------------------------------
// <copyright file="IdentityModule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using Karamchari.Identity.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Identity.Infrastructure;

public sealed class IdentityModule(IConfiguration configuration) : ICapabilityModule
{
    private readonly IConfiguration _configuration = configuration;

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariIdentity(_configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // Identity endpoints are currently registered manually in Program.cs 
        // or through a different mechanism. Adding hook here.
    }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        // Add identity consumers if any
    }
}
