// -----------------------------------------------------------------------
// <copyright file="PayrollModule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Payroll.DependencyInjection;

/// <summary>
/// Capability pack registration for the Payroll module.
/// </summary>
public sealed class PayrollModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    public PayrollModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariPayroll(_configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // Add Payroll endpoints here if they move out of Api.BFF.
    }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddKaramchariPayrollConsumers();
    }
}
