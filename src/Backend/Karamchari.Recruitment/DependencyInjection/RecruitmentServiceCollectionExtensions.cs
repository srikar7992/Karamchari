using Karamchari.Core.DependencyInjection;
using Karamchari.Recruitment.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Recruitment.DependencyInjection;

public static class RecruitmentServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariRecruitment(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // RLS setup
        services.RegisterTenantTable("Recruitment_JobRequisitions");
        services.RegisterTenantTable("Recruitment_CandidateProfiles");
        services.RegisterTenantTable("Recruitment_CandidateApplications");
        services.RegisterTenantTable("Recruitment_InterviewSessions");
        services.RegisterTenantTable("Recruitment_InterviewFeedback");
        services.RegisterTenantTable("Recruitment_OfferLetters");
        services.RegisterTenantTable("Recruitment_OfferApprovalSteps");

        services.AddDbContext<RecruitmentDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Missing connection string: {ConnectionStringName}");

            options.UseSqlServer(connectionString);
            options.AddKaramchariInterceptors(sp);
        });

        // Register domain services and orchestrators here

        return services;
    }
}
