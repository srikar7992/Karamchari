// -----------------------------------------------------------------------
// <copyright file="OnboardingModule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Onboarding.DependencyInjection;

public sealed class OnboardingModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public OnboardingModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariOnboarding(_configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariOnboardingConsumers();
    }
}
