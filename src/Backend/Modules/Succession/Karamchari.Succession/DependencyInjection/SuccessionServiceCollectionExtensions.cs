using Karamchari.Core.DependencyInjection;
using Karamchari.Succession.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Succession.DependencyInjection;

public static class SuccessionServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariSuccession(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.RegisterTenantTable("Succession_CriticalRoles");
        services.RegisterTenantTable("Succession_Plans");
        services.RegisterTenantTable("Succession_Candidates");
        services.RegisterTenantTable("Succession_TalentPools");
        services.RegisterTenantTable("Succession_TalentPoolMembers");
        services.RegisterTenantTable("Succession_CareerPaths");
        services.RegisterTenantTable("Succession_CareerSteps");

        services.AddDbContext<SuccessionDbContext>((sp, options) =>
        {
            var cs = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"ConnectionStrings:{ConnectionStringName} not configured.");
            options.UseSqlServer(cs, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null));
            options.AddKaramchariInterceptors(sp);
        });

        return services;
    }
}
