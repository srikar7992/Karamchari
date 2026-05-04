using Karamchari.Core.DependencyInjection;
using Karamchari.Payroll.Consumers;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.StateMachines;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Payroll.DependencyInjection;

/// <summary>
/// Extension methods for registering Payroll module services.
/// </summary>
public static class PayrollServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Adds the Payroll module services to the service collection and configures MassTransit.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="busConfigurator">The MassTransit bus registration configurator.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddKaramchariPayroll(
        this IServiceCollection services,
        IConfiguration configuration,
        IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(busConfigurator);

        // RLS: PayrollProfiles lives under each tenant's schema and must be covered by the
        // per-tenant security policy. Forgetting this line is a security regression.
        services.RegisterTenantTable("PayrollProfiles");
        services.RegisterTenantTable("PayrollRunStates");

        services.AddDbContext<PayrollDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before PayrollDbContext can be resolved.");

            options.UseSqlServer(connectionString);
            options.AddKaramchariInterceptors(serviceProvider);
        });

        busConfigurator.AddConsumer<EmployeeOnboardedConsumer>();
        busConfigurator.AddConsumer<CalculateAllEmployeePayConsumer>();
        
        busConfigurator.AddSagaStateMachine<PayrollRunStateMachine, PayrollRunState>()
            .EntityFrameworkRepository(r =>
            {
                r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                r.AddDbContext<DbContext, PayrollDbContext>();
            });

        return services;
    }
}
