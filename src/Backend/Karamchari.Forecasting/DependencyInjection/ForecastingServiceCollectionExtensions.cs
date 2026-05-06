using Karamchari.Forecasting.Persistence;
using Karamchari.Forecasting.Services;
using Karamchari.Core.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Forecasting.DependencyInjection;

public static class ForecastingServiceCollectionExtensions
{
    private const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddKaramchariForecasting(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.RegisterTenantTable("Forecast_Metrics");
        services.RegisterTenantTable("Forecast_ClientPaymentProfiles");

        services.AddDbContext<ForecastingDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName);
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<ForecastingEngine>();

        return services;
    }
}
