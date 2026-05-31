using Karamchari.Core.DependencyInjection;
using Karamchari.Recruitment.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Recruitment.DependencyInjection;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class RecruitmentServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static IServiceCollection AddKaramchariRecruitment(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator? busConfigurator = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (busConfigurator != null)
        {
            AddKaramchariRecruitmentConsumers(busConfigurator);
        }

        // RLS setup
        services.RegisterTenantTable("Recruitment_JobRequisitions");
        services.RegisterTenantTable("Recruitment_CandidateProfiles");
        services.RegisterTenantTable("Recruitment_CandidateApplications");
        services.RegisterTenantTable("Recruitment_InterviewSessions");
        services.RegisterTenantTable("Recruitment_InterviewFeedback");
        services.RegisterTenantTable("Recruitment_OfferLetters");

        services.AddDbContext<RecruitmentDbContext>((sp, options) =>
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
    /// Registers Recruitment module consumers for MassTransit.
    /// </summary>
    public static void AddKaramchariRecruitmentConsumers(this MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
    }
}
