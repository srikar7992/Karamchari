// -----------------------------------------------------------------------
// <copyright file="HelpdeskModule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Helpdesk.DependencyInjection;

public sealed class HelpdeskModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public HelpdeskModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariHelpdesk(_configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app) { }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<Karamchari.Helpdesk.Consumers.EmployeeTerminatedHelpdeskConsumer>();
    }
}
