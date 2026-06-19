// -----------------------------------------------------------------------
// <copyright file="BenefitsModule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.DependencyInjection;

using Karamchari.Benefits.Services;
using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Wires the Benefits Administration module into the modular monolith host.
/// Delegates endpoint mapping to the BFF layer (BenefitsEndpoints.MapBenefitsEndpoints).
/// </summary>
public sealed class BenefitsModule(IConfiguration configuration) : ICapabilityModule
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariBenefits(configuration);
        services.AddScoped<BenefitPlanQueryService>();
        services.AddScoped<EnrollmentQueryService>();
        services.AddScoped<EnrollmentCommandService>();
        services.AddScoped<LifeEventCommandService>();
        services.AddScoped<BenefitAdminService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app) { }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariBenefitsConsumers();
    }
}
