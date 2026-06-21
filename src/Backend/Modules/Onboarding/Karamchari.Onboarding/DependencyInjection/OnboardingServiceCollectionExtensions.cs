// -----------------------------------------------------------------------
// <copyright file="OnboardingServiceCollectionExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using Karamchari.Onboarding.Persistence;
using Karamchari.Onboarding.Sagas;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Onboarding.DependencyInjection;

public static class OnboardingServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariOnboarding(
        this IServiceCollection services,
        IConfiguration configuration,
        IBusRegistrationConfigurator? busConfigurator = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (busConfigurator != null)
            AddKaramchariOnboardingConsumers(busConfigurator);

        services.RegisterTenantTable("OnboardingCases");
        services.RegisterTenantTable("OnboardingTasks");
        services.RegisterTenantTable("OnboardingDocuments");
        services.RegisterTenantTable("OnboardingTemplates");
        services.RegisterTenantTable("OnboardingTemplateTasks");

        services.AddDbContext<OnboardingDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before OnboardingDbContext.");

            options.UseSqlServer(connectionString, sql =>
                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null));
            options.AddKaramchariInterceptors(sp);
        });

        return services;
    }

    public static void AddKaramchariOnboardingConsumers(this IBusRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.AddConsumer<Karamchari.Onboarding.Consumers.EmployeeHiredConsumer>();

        // H-2 priority 1: EmployeeOnboarding saga. Waits for BOTH payroll provisioning
        // and benefits enrollment before declaring an employee fully onboarded; escalates
        // (Incomplete) on timeout. In-memory repository for this increment; durable EF
        // persistence (EmployeeOnboardingSagaMap) + host-side UseDelayedMessageScheduler
        // are tracked follow-ups for production restart-survival.
        configurator.AddDelayedMessageScheduler();
        configurator.AddSagaStateMachine<EmployeeOnboardingSaga, EmployeeOnboardingSagaState>()
            .InMemoryRepository();
    }
}
