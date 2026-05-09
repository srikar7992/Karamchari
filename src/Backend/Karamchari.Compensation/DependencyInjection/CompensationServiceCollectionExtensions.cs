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
        MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(busConfigurator);

        // HR-visibility-only tables. All must be registered for RLS coverage.
        services.RegisterTenantTable("CompensationBands");
        services.RegisterTenantTable("MeritMatrices");
        services.RegisterTenantTable("MeritMatrixCells");
        services.RegisterTenantTable("IncrementBudgetPools");
        services.RegisterTenantTable("EmployeeCompensationRecords");
        services.RegisterTenantTable("CompensationRevisions");

        services.AddDbContext<CompensationDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before CompensationDbContext can be resolved.");

            options.UseSqlServer(connectionString);
            options.AddKaramchariInterceptors(serviceProvider);
        });

        return services;
    }
}
