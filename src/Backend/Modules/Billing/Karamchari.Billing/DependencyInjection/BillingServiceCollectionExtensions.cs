// -----------------------------------------------------------------------
// <copyright file="BillingServiceCollectionExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator? busConfigurator = null)
    {
        if (busConfigurator != null)
        {
            AddKaramchariBillingConsumers(busConfigurator);
        }
        // RLS Table Registrations
        services.RegisterTenantTable("Billing_Contracts");
        services.RegisterTenantTable("Billing_BillableEntries");
        services.RegisterTenantTable("Billing_Invoices");

        services.AddDbContext<BillingDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before BillingDbContext can be resolved.");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
            options.AddKaramchariInterceptors(serviceProvider);
        });

        services.AddScoped<Karamchari.Billing.Services.InvoiceGeneratorService>();
        services.AddScoped<Karamchari.Billing.Services.ARAnalyticsService>();
        services.AddScoped<Karamchari.Billing.Contracts.IBillingReadService, Karamchari.Billing.Services.BillingReadService>();

        return services;
    }

    /// <summary>
    /// Registers Billing module consumers for MassTransit.
    /// </summary>
    public static void AddKaramchariBillingConsumers(this MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
        busConfigurator.AddConsumer<Karamchari.Billing.Consumers.BillableEntryConsumer>();
        busConfigurator.AddConsumer<Karamchari.Billing.Consumers.CollectionCaseConsumer>();
    }
}
