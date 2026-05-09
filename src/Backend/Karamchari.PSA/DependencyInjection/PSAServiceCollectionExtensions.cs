namespace Karamchari.PSA.DependencyInjection;

using Karamchari.PSA.Persistence;
using Karamchari.PSA.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the PSA bounded context services.
/// </summary>
public static class PSAServiceCollectionExtensions
{
    /// <summary>
    /// Registers all PSA domain services, the DbContext, and the MassTransit consumer.
    /// </summary>
    public static IServiceCollection AddPSA(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext â€” same connection string as other contexts; RLS scopes by tenant.
        services.AddDbContext<PSADbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("KaramchariDb")));

        // Domain services
        services.AddScoped<ProjectResourceRepository>();
        services.AddScoped<InvoiceGeneratorService>();

        return services;
    }
}
