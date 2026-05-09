using Karamchari.Billing.Persistence;
using Karamchari.Core.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Billing.DependencyInjection;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class BillingServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static IServiceCollection AddKaramchariBilling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // RLS Table Registrations
        services.RegisterTenantTable("Billing_Contracts");
        services.RegisterTenantTable("Billing_BillableEntries");
        services.RegisterTenantTable("Billing_Invoices");

        services.AddDbContext<BillingDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before BillingDbContext can be resolved.");

            options.UseSqlServer(connectionString);
            options.AddKaramchariInterceptors(serviceProvider);
        });

        services.AddScoped<Karamchari.Billing.Services.InvoiceGeneratorService>();
        services.AddScoped<Karamchari.Billing.Services.ARAnalyticsService>();

        services.AddHostedService<Karamchari.Billing.Services.CollectionsBackgroundWorker>();

        return services;
    }
}
