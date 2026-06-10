// -----------------------------------------------------------------------
// <copyright file="FinancialOpsServiceCollectionExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using Karamchari.Core.Persistence;
using Karamchari.FinancialOps.Domain.Chaos;
using Karamchari.FinancialOps.Domain.Ledger;
using Karamchari.FinancialOps.Domain.Settlements;
using Karamchari.FinancialOps.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.FinancialOps.DependencyInjection;

/// <summary>
/// Extension methods for registering Financial Operations module services.
/// </summary>
public static class FinancialOpsServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Adds the Financial Operations module services to the service collection.
    /// </summary>
    public static IServiceCollection AddKaramchariFinancialOps(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Security: Register tenant-owned tables for RLS coverage.
        services.RegisterTenantTable("WorkforceFinancialLedger");
        services.RegisterTenantTable("FinancialOperationalPeriods");
        services.RegisterTenantTable("Financial_EmployeeLoans");
        services.RegisterTenantTable("Financial_EmployeeAdvances");
        services.RegisterTenantTable("FinancialCompensations");
        services.RegisterTenantTable("ReimbursementSettlementBatches");
        services.RegisterTenantTable("ExpenseClaims");
        services.RegisterTenantTable("ExpenseCategories");

        services.AddDbContext<FinancialOpsDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before FinancialOpsDbContext can be resolved.");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
            options.AddKaramchariInterceptors(serviceProvider);
        });

        services.AddScoped<ISettlementExecutionProvider, ManualSettlementExecutionProvider>();
        services.AddScoped<IFinancialLedger, FinancialLedger>();
        services.AddScoped<IFinancialReplayService, FinancialReplayService>();
        services.AddScoped<IFinancialSequencePolicy, FinancialExecutionSequencePolicy>();
        services.AddScoped<IFinancialConsistencyGuard, FinancialConsistencyGuard>();

        // Phase 1E.11: Chaos Injection Infrastructure
        services.AddSingleton<ChaosExecutionTimeline>();
        services.AddScoped<IFinancialChaosEngine, FinancialChaosEngine>();

        return services;
    }
}
