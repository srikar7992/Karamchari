using Karamchari.Core.DependencyInjection;
using Karamchari.Intelligence.Consumers;
using Karamchari.Intelligence.Persistence;
using Karamchari.Intelligence.Services;
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
        MassTransit.IBusRegistrationConfigurator? busConfigurator = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (busConfigurator != null)
        {
            AddKaramchariIntelligenceConsumers(busConfigurator);
        }

        // RLS setup
        services.RegisterTenantTable("Intelligence_Signals");
        services.RegisterTenantTable("Intelligence_MetricDefinitions");
        services.RegisterTenantTable("Strategy_OrgHealthSignals");
        services.RegisterTenantTable("Strategy_WorkforceRiskSignals");
        services.RegisterTenantTable("Strategy_ExecutiveInsights");
        services.RegisterTenantTable("Strategy_StrategicScenarios");
        // Phase 6
        services.RegisterTenantTable("Intel_WorkforceSignals");
        services.RegisterTenantTable("Intel_BurnoutScores");
        services.RegisterTenantTable("Intel_AttritionScores");
        services.RegisterTenantTable("Intel_WorkforceHealthScores");
        services.RegisterTenantTable("Intel_ManagerScores");
        services.RegisterTenantTable("Intel_WorkforceRecommendations");
        // Phase 6.1
        services.RegisterTenantTable("Intel_ScoreSnapshots");

        services.RegisterTenantTable("Intel_Forecasts");
        services.RegisterTenantTable("Intel_TalentRiskScores");
        services.RegisterTenantTable("Intel_WorkloadFairness");
        services.RegisterTenantTable("Intel_AbsenceContagion");
        services.RegisterTenantTable("Intel_FeatureSnapshots");
        services.RegisterTenantTable("Intel_OutcomeLabels");
        // Phase 6.2
        services.RegisterTenantTable("Intel_ScoreAudits");
        services.RegisterTenantTable("Intel_InterventionOutcomes");
        services.RegisterTenantTable("Intel_WorkforceHotspots");
        // Phase 6.3
        services.RegisterTenantTable("Intel_DependencyScores");
        services.RegisterTenantTable("Intel_ScheduleQualityScores");
        services.RegisterTenantTable("Intel_EmployeeSegments");

        services.AddDbContext<IntelligenceDbContext>((sp, options) =>
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

        services.AddScoped<Karamchari.Intelligence.Services.IOrganizationalHealthEngine,
                            Karamchari.Intelligence.Services.OrganizationalHealthEngine>();
        services.AddScoped<Karamchari.Intelligence.Services.IWorkforceRiskEngine,
                            Karamchari.Intelligence.Services.WorkforceRiskEngine>();
        services.AddScoped<Karamchari.Intelligence.Services.IExecutiveDecisionService,
                            Karamchari.Intelligence.Services.ExecutiveDecisionService>();
        services.AddScoped<Karamchari.Intelligence.Services.IWorkforcePlanningService,
                            Karamchari.Intelligence.Services.WorkforcePlanningService>();

        // Phase 6
        services.AddScoped<WorkforceSignalService>();
        services.AddHostedService<WorkforceIntelligenceRecomputeJob>();
        // Phase 6.1
        services.AddScoped<WorkforceForecasterService>();
        services.AddScoped<OutcomeLabelService>();
        services.AddHostedService<FeatureSnapshotJob>();
        // Phase 6.2
        services.AddScoped<HotspotDetectionService>();
        services.AddScoped<InterventionTrackerService>();
        services.AddHostedService<FeatureSnapshotRetentionJob>();
        // Phase 6.3
        services.AddScoped<DependencyRiskService>();
        services.AddScoped<ScheduleQualityService>();
        services.AddScoped<RecommendationEffectivenessService>();

        return services;
    }

    /// <summary>
    /// Registers Intelligence module consumers for MassTransit.
    /// </summary>
    public static void AddKaramchariIntelligenceConsumers(this MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
        busConfigurator.AddConsumer<WorkforceIntelligenceConsumer>();
    }
}
