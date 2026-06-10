// -----------------------------------------------------------------------
// <copyright file="ComplianceModule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Compliance.DependencyInjection;

public sealed class ComplianceModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public ComplianceModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariCompliance(_configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariComplianceConsumers();
    }
}
