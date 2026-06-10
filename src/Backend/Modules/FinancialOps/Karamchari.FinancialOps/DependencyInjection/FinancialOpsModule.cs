// -----------------------------------------------------------------------
// <copyright file="FinancialOpsModule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using Karamchari.FinancialOps.Domain.Reimbursements;
using Karamchari.FinancialOps.Persistence;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.FinancialOps.DependencyInjection;

/// <summary>
/// Module definition for the Financial Operations bounded context.
/// </summary>
public sealed class FinancialOpsModule : ICapabilityModule
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="FinancialOpsModule"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public FinancialOpsModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public void RegisterServices(IServiceCollection services)
    {
        services.AddKaramchariFinancialOps(_configuration);
    }

    /// <inheritdoc/>
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // Future: Register BFF endpoints here
    }

    /// <inheritdoc/>
    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.AddConsumer<SubmitExpenseClaimConsumer>();
    }
}
