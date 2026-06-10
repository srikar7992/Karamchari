// -----------------------------------------------------------------------
// <copyright file="ComplianceServiceCollectionExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compliance.Consumers;
using Karamchari.Compliance.Contracts;
using Karamchari.Compliance.Persistence;
using Karamchari.Compliance.Services;
using Karamchari.Core.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Compliance.DependencyInjection;

public static class ComplianceServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariCompliance(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator? busConfigurator = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (busConfigurator != null)
        {
            AddKaramchariComplianceConsumers(busConfigurator);
        }

        services.RegisterTenantTable("Compliance_RetentionPolicies");
        services.RegisterTenantTable("Compliance_RetentionActions");
        services.RegisterTenantTable("Compliance_LegalHolds");
        services.RegisterTenantTable("Compliance_LegalHoldScopes");
        services.RegisterTenantTable("Compliance_RegulatoryRegisters");
        services.RegisterTenantTable("Compliance_RegulatoryRules");
        services.RegisterTenantTable("Compliance_PolicyViolations");
        services.RegisterTenantTable("Compliance_ComplianceScores");
        services.RegisterTenantTable("Compliance_ComplianceSnapshots");
        services.RegisterTenantTable("Compliance_AuditPackages");
        services.RegisterTenantTable("Compliance_ScoreHistory");

        services.AddDbContext<ComplianceDbContext>((sp, options) =>
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

        services.AddScoped<IComplianceGateService, ComplianceGateService>();
        services.AddScoped<PolicyViolationDetectionService>();
        services.AddScoped<ComplianceScoringService>();
        services.AddScoped<AuditPackageService>();
        services.AddScoped<ComplianceDashboardQueryService>();
        services.AddScoped<LegalHoldService>();
        services.AddScoped<RetentionExecutionService>();
        services.AddScoped<RegulatoryRuleEvaluator>();
        services.AddScoped<ComplianceTrendService>();
        services.AddScoped<ComplianceSnapshotService>();
        services.AddHostedService<RetentionEvaluationJob>();

        return services;
    }

    public static void AddKaramchariComplianceConsumers(this MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
        busConfigurator.AddConsumer<ComplianceEventConsumer>();
        busConfigurator.AddConsumer<PayrollComplianceConsumer>();
    }
}
