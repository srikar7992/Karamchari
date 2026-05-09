using Karamchari.Core.DependencyInjection;
using Karamchari.Intelligence.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Intelligence.DependencyInjection;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class IntelligenceServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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
        services.RegisterTenantTable("Strategy_OrgHealthSignals");
        services.RegisterTenantTable("Strategy_WorkforceRiskSignals");
        services.RegisterTenantTable("Strategy_ExecutiveInsights");
        services.RegisterTenantTable("Strategy_StrategicScenarios");

        services.AddDbContext<IntelligenceDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Missing connection string: {ConnectionStringName}");

            options.UseSqlServer(connectionString);
            options.AddKaramchariInterceptors(sp);
        });

        services.AddScoped<Karamchari.Intelligence.Services.IOrganizationalHealthEngine,
                            Karamchari.Intelligence.Services.OrganizationalHealthEngine>();
        services.AddScoped<Karamchari.Intelligence.Services.IWorkforceRiskEngine,
                            Karamchari.Intelligence.Services.WorkforceRiskEngine>();
        services.AddScoped<Karamchari.Intelligence.Services.IExecutiveDecisionService,
                            Karamchari.Intelligence.Services.ExecutiveDecisionService>();
        services.AddScoped<Karamchari.Intelligence.Services.IWorkforcePlanningService,
                            Karamchari.Intelligence.Services.WorkforcePlanningService>();

        // Background Workers
        services.AddHostedService<Karamchari.Intelligence.Services.DriftDetectionWorker>();

        return services;
    }
}
