using Karamchari.Core.DependencyInjection;
using Karamchari.PSA.Hubs;
using Karamchari.PSA.Persistence;
using Karamchari.PSA.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Karamchari.PSA.DependencyInjection;

/// <summary>
/// Extension methods for registering PSA module services.
/// </summary>
public static class PSAServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariPSA(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator? busConfigurator = null)
    {
        if (busConfigurator != null)
        {
            AddKaramchariPSAConsumers(busConfigurator);
        }

        services.AddDbContext<PSADbContext>((serviceProvider, o) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName);
            o.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
            o.AddKaramchariInterceptors(serviceProvider);
        });

        services.AddScoped<ProjectResourceRepository>();
        services.AddScoped<InvoiceGeneratorService>();
        services.AddScoped<EmployeeCostService>();
        services.AddSingleton<PricingEngine>();
        services.AddSingleton<SimulationService>();
        services.AddSingleton<AnomalyDetectionService>();
        services.AddScoped<CashFlowService>();
        services.AddSingleton<AnalyticsBroadcaster>();

        return services;
    }

    public static void AddKaramchariPSAConsumers(this MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
        busConfigurator.AddConsumer<Karamchari.PSA.Consumers.BillableRevenueConsumer>();
        busConfigurator.AddConsumer<Karamchari.PSA.Consumers.ProfitCalculationConsumer>();
    }
}
