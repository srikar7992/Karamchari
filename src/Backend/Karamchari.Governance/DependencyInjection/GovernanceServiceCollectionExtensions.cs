using Karamchari.Core.DependencyInjection;
using Karamchari.Governance.Persistence;
using Karamchari.Governance.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Governance.DependencyInjection;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class GovernanceServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static IServiceCollection AddKaramchariGovernance(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator? busConfigurator = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (busConfigurator != null)
        {
            AddKaramchariGovernanceConsumers(busConfigurator);
        }

        // RLS setup
        services.RegisterTenantTable("Governance_ServiceLevelObjectives");
        services.RegisterTenantTable("Governance_OperationalIncidents");
        services.RegisterTenantTable("Governance_SchemaDefinitions");

        services.AddDbContext<GovernanceDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Missing connection string: {ConnectionStringName}");

            options.UseSqlServer(connectionString);
            options.AddKaramchariInterceptors(sp);
        });

        services.AddScoped<ISchemaValidator, SchemaValidator>();

        return services;
    }

    /// <summary>
    /// Registers Governance module consumers for MassTransit.
    /// </summary>
    public static void AddKaramchariGovernanceConsumers(this MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
    }
}
