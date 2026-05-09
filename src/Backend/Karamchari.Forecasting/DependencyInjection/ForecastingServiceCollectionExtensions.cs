using Karamchari.Forecasting.Persistence;
using Karamchari.Forecasting.Services;
using Karamchari.Core.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Forecasting.DependencyInjection;

public static class ForecastingServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariForecasting(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.RegisterTenantTable("Forecast_Metrics");
        services.RegisterTenantTable("Forecast_ClientPaymentProfiles");

        services.AddDbContext<ForecastingDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before ForecastingDbContext can be resolved.");

            options.UseSqlServer(connectionString);
            options.AddKaramchariInterceptors(serviceProvider);
        });

        services.AddScoped<ForecastingEngine>();

        return services;
    }
}
