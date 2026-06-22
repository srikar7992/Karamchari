// -----------------------------------------------------------------------
// <copyright file="ForecastingModule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using Karamchari.Forecasting.Services;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Forecasting.DependencyInjection;

public sealed class ForecastingModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public ForecastingModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariForecasting(_configuration);
        services.AddHostedService<HeadcountBudgetReconciler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app) { }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariForecastingConsumers();
    }
}
