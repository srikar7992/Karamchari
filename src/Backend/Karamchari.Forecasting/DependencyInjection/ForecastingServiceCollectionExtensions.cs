using Karamchari.Core.DependencyInjection;
using Karamchari.Forecasting.Persistence;
using Karamchari.Forecasting.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Forecasting.DependencyInjection;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class ForecastingServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static IServiceCollection AddKaramchariForecasting(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator? busConfigurator = null)
    {
        if (busConfigurator != null)
        {
            AddKaramchariForecastingConsumers(busConfigurator);
        }

        services.RegisterTenantTable("Forecast_Metrics");
        services.RegisterTenantTable("Forecast_ClientPaymentProfiles");

        services.AddDbContext<ForecastingDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before ForecastingDbContext can be resolved.");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
            options.AddKaramchariInterceptors(serviceProvider);
        });

        services.AddScoped<ForecastingEngine>();

        return services;
    }

    /// <summary>
    /// Registers Forecasting module consumers for MassTransit.
    /// </summary>
    public static void AddKaramchariForecastingConsumers(this MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
        busConfigurator.AddConsumer<Karamchari.Forecasting.Consumers.ForecastUpdateConsumer>();
    }
}
