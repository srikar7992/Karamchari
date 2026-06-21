// -----------------------------------------------------------------------
// <copyright file="GovernanceServiceCollectionExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using Karamchari.Core.Persistence;
using Karamchari.Governance.Infrastructure;
using Karamchari.Governance.Persistence;
using Karamchari.Governance.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Governance.DependencyInjection;

public static class GovernanceServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariGovernance(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator? busConfigurator = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (busConfigurator != null)
            AddKaramchariGovernanceConsumers(busConfigurator);

        services.RegisterTenantTable("Governance_ServiceLevelObjectives");
        services.RegisterTenantTable("Governance_OperationalIncidents");
        services.RegisterTenantTable("Governance_SchemaDefinitions");
        services.RegisterTenantTable("Governance_SodMatrix");
        services.RegisterTenantTable("Governance_SodViolations");

        services.AddScoped<AuditInterceptor>();
        services.AddScoped<IFinancialAuditInterceptor>(sp => sp.GetRequiredService<AuditInterceptor>());

        services.AddDbContext<GovernanceDbContext>((sp, options) =>
        {
            var cs = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Missing connection string: {ConnectionStringName}");
            options.UseSqlServer(cs, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null));
            options.AddKaramchariInterceptors(sp);
        });

        services.AddScoped<ISchemaValidator, SchemaValidator>();
        services.AddScoped<SodEnforcementService>();
        services.AddScoped<AuditQueryService>();

        return services;
    }

    public static void AddKaramchariGovernanceConsumers(this MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
    }
}
