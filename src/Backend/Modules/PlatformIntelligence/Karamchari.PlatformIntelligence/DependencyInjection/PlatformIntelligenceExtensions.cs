using Karamchari.Core.DependencyInjection;
using Karamchari.PlatformIntelligence.Persistence;
using Karamchari.PlatformIntelligence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.PlatformIntelligence.DependencyInjection;

public static class PlatformIntelligenceExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariPlatformIntelligence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.RegisterTenantTable("Platform_Decisions");
        services.RegisterTenantTable("Platform_Scenarios");
        services.RegisterTenantTable("Platform_SimulationRuns");
        services.RegisterTenantTable("Platform_Optimizations");
        services.RegisterTenantTable("Platform_ExecutiveDigests");
        services.RegisterTenantTable("Platform_WorkforceRisks");
        services.RegisterTenantTable("Platform_Recommendations");

        services.AddDbContext<PlatformIntelligenceDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Missing connection string: {ConnectionStringName}");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
            options.AddKaramchariInterceptors(sp);
        });

        services.AddScoped<CrossDomainSignalAggregator>();
        services.AddScoped<PlatformDecisionService>();
        services.AddScoped<ScenarioEvaluationService>();
        services.AddScoped<WorkforceSimulationService>();
        services.AddScoped<WorkforceOptimizationService>();
        services.AddScoped<ExecutiveDigestService>();
        services.AddScoped<WorkforceRiskService>();
        services.AddScoped<RecommendationService>();

        return services;
    }
}
