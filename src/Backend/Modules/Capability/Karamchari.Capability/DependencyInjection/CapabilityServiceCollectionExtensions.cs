using Karamchari.Capability.Persistence;
using Karamchari.Core.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Capability.DependencyInjection;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class CapabilityServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static IServiceCollection AddKaramchariCapability(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator? busConfigurator = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (busConfigurator != null)
        {
            AddKaramchariCapabilityConsumers(busConfigurator);
        }

        // RLS setup
        services.RegisterTenantTable("Capability_SkillDefinitions");
        services.RegisterTenantTable("Capability_Profiles");
        services.RegisterTenantTable("Capability_VerifiedSkills");
        services.RegisterTenantTable("Capability_LearningModules");
        services.RegisterTenantTable("Capability_LearningEnrollments");
        services.RegisterTenantTable("Capability_CertificationAchievements");
        services.RegisterTenantTable("Capability_GrowthPlans");
        services.RegisterTenantTable("Capability_GrowthMilestones");
        services.RegisterTenantTable("Capability_SkillCategories");
        services.RegisterTenantTable("Capability_CertificationDefinitions");
        services.RegisterTenantTable("Capability_RoleSkillRequirements");
        services.RegisterTenantTable("Capability_RequiredSkills");
        services.RegisterTenantTable("Capability_ModuleSkillTargets");

        services.AddDbContext<CapabilityDbContext>((sp, options) =>
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

        // Register domain services and orchestrators here

        return services;
    }

    /// <summary>
    /// Registers Capability module consumers for MassTransit.
    /// </summary>
    public static void AddKaramchariCapabilityConsumers(this MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
        busConfigurator.AddConsumer<Karamchari.Capability.Consumers.LearningEnrollmentCompletedConsumer>();
    }
}
