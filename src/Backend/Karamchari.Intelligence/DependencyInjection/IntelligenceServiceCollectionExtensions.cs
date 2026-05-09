using Karamchari.Core.DependencyInjection;
using Karamchari.Intelligence.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Intelligence.DependencyInjection;

public static class IntelligenceServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariIntelligence(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // RLS setup
        services.RegisterTenantTable("Intelligence_Signals");
        services.RegisterTenantTable("Intelligence_MetricDefinitions");

        services.AddDbContext<IntelligenceDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Missing connection string: {ConnectionStringName}");

            options.UseSqlServer(connectionString);
            options.AddKaramchariInterceptors(sp);
        });

        // Background Workers
        services.AddHostedService<Karamchari.Intelligence.Services.DriftDetectionWorker>();

        return services;
    }
}
