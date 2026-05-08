using Karamchari.Core.DependencyInjection;
using Karamchari.Core.Messaging;
using Karamchari.HR.Contracts.Employees;
using Karamchari.HR.Contracts.Organization;
using Karamchari.HR.Messaging;
using Karamchari.HR.Persistence;
using Karamchari.HR.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Karamchari.HR.DependencyInjection;

/// <summary>
/// Extension methods for registering HR module services.
/// </summary>
public static class HRServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Adds the HR module services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="busConfigurator">The MassTransit bus registration configurator.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddKaramchariHR(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(busConfigurator);

        busConfigurator.AddConsumer<Karamchari.HR.Consumers.TenantProvisionedConsumer>();

        // RLS: Employees lives under each tenant's schema and must be covered by the
        // per-tenant security policy. Forgetting this line is a security regression.
        services.RegisterTenantTable("Departments");
        services.RegisterTenantTable("Employees");
        services.RegisterTenantTable("EmployeeRelationships");

        // Replace the Core-shipped fail-closed null dispatcher with the
        // MassTransit-backed publisher. Replace (not Add) ensures we win even
        // if AddKaramchariCore registered the null implementation first.
        services.Replace(ServiceDescriptor.Scoped<IDomainEventDispatcher, MassTransitDomainEventDispatcher>());
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IVisibilityResolver, VisibilityResolver>();
        
        services.Configure<DocumentIntelligenceOptions>(
            configuration.GetSection(DocumentIntelligenceOptions.SectionName));
        services.AddScoped<IDocumentIntelligenceService, DocumentIntelligenceService>();

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
