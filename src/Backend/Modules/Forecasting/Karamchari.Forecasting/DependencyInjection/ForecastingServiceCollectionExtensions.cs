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

        // Phase 5: Workforce Planning tables
        services.RegisterTenantTable("WFP_EmployeeIndex");
        services.RegisterTenantTable("WFP_SkillExpiries");
        services.RegisterTenantTable("WFP_AttendanceSnapshots");
        services.RegisterTenantTable("WFP_AttritionSnapshots");
        services.RegisterTenantTable("WFP_Projections");
        services.RegisterTenantTable("WFP_ApprovedLeaves");
        services.RegisterTenantTable("WFP_DemandForecasts");
        services.RegisterTenantTable("WFP_SupplyForecasts");
        services.RegisterTenantTable("WFP_CoverageRisks");
        services.RegisterTenantTable("WFP_HiringGaps");
        services.RegisterTenantTable("WFP_ScenarioForecasts");
        services.RegisterTenantTable("WFP_ScenarioResults");
        // Phase 5.1
        services.RegisterTenantTable("WFP_SkillSubstitutions");
        services.RegisterTenantTable("WFP_OvertimePolicies");
        services.RegisterTenantTable("WFP_ForecastAccuracies");
        services.RegisterTenantTable("WFP_RecomputeQueue");
        // Phase 5.x Enterprise Enhancements
        services.RegisterTenantTable("WFP_FutureHires");
        services.RegisterTenantTable("WFP_ContractExpiries");
        services.RegisterTenantTable("WFP_RetirementRisks");
        // Phase 5.2
        services.RegisterTenantTable("WFP_RetirementPolicies");

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

        // Phase 5: Workforce Planning
        services.AddScoped<Karamchari.Forecasting.Services.DemandForecastGenerator>();
        services.AddScoped<Karamchari.Forecasting.Services.SupplyForecastCalculator>();
        services.AddScoped<Karamchari.Forecasting.Services.ScenarioEngine>();
        services.AddScoped<Karamchari.Forecasting.Services.WorkforcePlanningService>();
        services.AddScoped<Karamchari.Forecasting.Services.ForecastAccuracyCalculator>();
        services.AddHostedService<Karamchari.Forecasting.Services.WorkforceRecomputeJob>();
        services.AddHostedService<Karamchari.Forecasting.Services.RecomputeQueueDrainer>();

        return services;
    }

    /// <summary>
    /// Registers Forecasting module consumers for MassTransit.
    /// </summary>
    public static void AddKaramchariForecastingConsumers(this MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
        busConfigurator.AddConsumer<Karamchari.Forecasting.Consumers.ForecastUpdateConsumer>();
        busConfigurator.AddConsumer<Karamchari.Forecasting.Consumers.WorkforcePlanningConsumer>();
    }
}
