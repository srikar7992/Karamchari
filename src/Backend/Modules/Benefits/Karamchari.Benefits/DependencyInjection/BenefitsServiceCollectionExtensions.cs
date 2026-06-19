// -----------------------------------------------------------------------
// <copyright file="BenefitsServiceCollectionExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.DependencyInjection;

using Karamchari.Benefits.Consumers;
using Karamchari.Benefits.Persistence;
using Karamchari.Core.DependencyInjection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Benefits Administration module services.
/// </summary>
public static class BenefitsServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Adds the Benefits Administration module to the service collection.
    /// </summary>
    public static IServiceCollection AddKaramchariBenefits(
        this IServiceCollection services,
        IConfiguration configuration,
        IBusRegistrationConfigurator? busConfigurator = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (busConfigurator != null)
            AddKaramchariBenefitsConsumers(busConfigurator);

        // RLS tables — one entry per DbSet so the schema-rewriting interceptor
        // can apply the tenant filter on every query.
        services.RegisterTenantTable("BenefitPlans");
        services.RegisterTenantTable("BenefitPlanTiers");
        services.RegisterTenantTable("BenefitEnrollmentWindows");
        services.RegisterTenantTable("BenefitEnrollments");
        services.RegisterTenantTable("BenefitElections");
        services.RegisterTenantTable("BenefitDependents");
        services.RegisterTenantTable("BenefitLifeEvents");

        services.AddDbContext<BenefitsDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before BenefitsDbContext can be resolved.");

            options.UseSqlServer(connectionString, sql =>
                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null));
            options.AddKaramchariInterceptors(sp);
        });

        return services;
    }

    /// <summary>Registers Benefits module consumers for MassTransit.</summary>
    public static void AddKaramchariBenefitsConsumers(this IBusRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.AddConsumer<EmployeeOnboardedBenefitsConsumer>();
    }
}
