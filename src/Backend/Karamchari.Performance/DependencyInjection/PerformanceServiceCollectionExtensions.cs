using Karamchari.Core.DependencyInjection;
using Karamchari.Performance.Consumers;
using Karamchari.Performance.Domain.Scoring;
using Karamchari.Performance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Performance.DependencyInjection;

/// <summary>
/// Registers all Performance bounded-context services.
/// Call after AddKaramchariCore.
/// </summary>
public static class PerformanceServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static IServiceCollection AddKaramchariPerformance(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator? busConfigurator = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (busConfigurator != null)
        {
            AddKaramchariPerformanceConsumers(busConfigurator);
        }

        // RLS: every table that holds tenant data must be registered here.
        // Forgetting any of these is a security regression â€” RLS won't cover the table.
        services.RegisterTenantTable("GoalCycles");
        services.RegisterTenantTable("Goals");
        services.RegisterTenantTable("GoalProgressUpdates");
        services.RegisterTenantTable("GoalRevisionRecords");

        services.RegisterTenantTable("OKRCycles");
        services.RegisterTenantTable("Objectives");
        services.RegisterTenantTable("KeyResults");
        services.RegisterTenantTable("KRCheckIns");

        services.RegisterTenantTable("KPIDefinitions");
        services.RegisterTenantTable("KPIResults");
        services.RegisterTenantTable("KPISnapshots");

        services.RegisterTenantTable("ReviewTemplates");
        services.RegisterTenantTable("ReviewCycles");
        services.RegisterTenantTable("ReviewAssignments");
        services.RegisterTenantTable("ReviewSubmissions");
        services.RegisterTenantTable("ReviewResponses");

        services.RegisterTenantTable("FeedbackRequests");
        services.RegisterTenantTable("FeedbackSubmissions");

        services.RegisterTenantTable("CalibrationSessions");
        services.RegisterTenantTable("CalibrationPanelMembers");
        services.RegisterTenantTable("CalibrationEntries");
        services.RegisterTenantTable("CalibrationAdjustmentRecords");

        services.RegisterTenantTable("PromotionRecommendations");
        services.RegisterTenantTable("PromotionApprovalActions");

        services.RegisterTenantTable("SkillTaxonomies");
        services.RegisterTenantTable("SkillCategories");
        services.RegisterTenantTable("Skills");
        services.RegisterTenantTable("EmployeeSkillProfiles");
        services.RegisterTenantTable("EmployeeSkillAssessments");

        services.RegisterTenantTable("CareerFrameworks");
        services.RegisterTenantTable("CareerTracks");
        services.RegisterTenantTable("CareerLevels");
        services.RegisterTenantTable("EmployeeGrowthPlans");
        services.RegisterTenantTable("DevelopmentActions");

        services.RegisterTenantTable("PerformanceSnapshots");

        // CQRS projection tables (ADR-0009)
        services.RegisterTenantTable("ManagerDashboardProjections");
        services.RegisterTenantTable("ReviewTaskInboxItems");
        services.RegisterTenantTable("CalibrationBoardProjections");
        services.RegisterTenantTable("PromotionPipelineItems");
        services.RegisterTenantTable("TalentHeatmapEntries");
        services.RegisterTenantTable("TeamGoalSummaries");
        services.RegisterTenantTable("EmployeeSkillInventoryItems");

        // Reporting (ADR-0013)
        services.RegisterTenantTable("ExportJobs");

        // Scoring strategies â€” pluggable per tenant in a future iteration
        services.AddScoped<IOKRScoringStrategy, DefaultOKRScoringStrategy>();

        services.AddDbContext<PerformanceDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before PerformanceDbContext can be resolved.");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
            options.AddKaramchariInterceptors(serviceProvider);
        });

        return services;
    }

    /// <summary>
    /// Registers Performance module consumers for MassTransit.
    /// </summary>
    public static void AddKaramchariPerformanceConsumers(this MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
        busConfigurator.AddConsumer<EmployeeOnboardedPerformanceConsumer>();
    }
}
