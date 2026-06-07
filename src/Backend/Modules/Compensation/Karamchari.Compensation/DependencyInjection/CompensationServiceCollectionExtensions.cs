using Karamchari.Compensation.Persistence;
using Karamchari.Core.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Compensation.DependencyInjection;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class CompensationServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static IServiceCollection AddKaramchariCompensation(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator? busConfigurator = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (busConfigurator != null)
        {
            AddKaramchariCompensationConsumers(busConfigurator);
        }

        // HR-visibility-only tables. All must be registered for RLS coverage.
        services.RegisterTenantTable("CompensationBands");
        services.RegisterTenantTable("MeritMatrices");
        services.RegisterTenantTable("MeritMatrixCells");
        services.RegisterTenantTable("IncrementBudgetPools");
        services.RegisterTenantTable("EmployeeCompensationRecords");
        services.RegisterTenantTable("CompensationRevisions");
        services.RegisterTenantTable("BonusPlans");
        services.RegisterTenantTable("BonusAllocations");

        services.AddDbContext<CompensationDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before CompensationDbContext can be resolved.");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
            options.AddKaramchariInterceptors(serviceProvider);
        });

        return services;
    }

    /// <summary>
    /// Registers Compensation module consumers for MassTransit.
    /// </summary>
    public static void AddKaramchariCompensationConsumers(this MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
    }
}
