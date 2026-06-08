using Karamchari.Core.DependencyInjection;
using Karamchari.Onboarding.Persistence;
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
    }
}
