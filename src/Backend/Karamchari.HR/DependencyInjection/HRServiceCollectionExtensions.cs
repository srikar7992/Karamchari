using Karamchari.Core.DependencyInjection;
using Karamchari.Core.Messaging;
using Karamchari.HR.Messaging;
using Karamchari.HR.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Karamchari.HR.DependencyInjection;

public static class HRServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariHR(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // RLS: Employees lives under each tenant's schema and must be covered by the
        // per-tenant security policy. Forgetting this line is a security regression.
        services.RegisterTenantTable("Employees");

        // Replace the Core-shipped fail-closed null dispatcher with the
        // MassTransit-backed publisher. Replace (not Add) ensures we win even
        // if AddKaramchariCore registered the null implementation first.
        services.Replace(ServiceDescriptor.Scoped<IDomainEventDispatcher, MassTransitDomainEventDispatcher>());

        services.AddDbContext<HRDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before HRDbContext can be resolved.");

            options.UseSqlServer(connectionString);
            options.AddKaramchariInterceptors(serviceProvider);
        });

        return services;
    }
}
