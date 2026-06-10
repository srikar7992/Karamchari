// -----------------------------------------------------------------------
// <copyright file="AssetManagementModule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.AssetManagement.DependencyInjection;
using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.AssetManagement.DependencyInjection;

public sealed class AssetManagementModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public AssetManagementModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariAssetManagement(_configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app) { }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<Karamchari.AssetManagement.Consumers.EmployeeTerminatedAssetConsumer>();
    }
}
