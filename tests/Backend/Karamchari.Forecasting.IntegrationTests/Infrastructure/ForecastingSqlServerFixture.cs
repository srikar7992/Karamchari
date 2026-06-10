// -----------------------------------------------------------------------
// <copyright file="ForecastingSqlServerFixture.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Forecasting.Consumers;
using Karamchari.Forecasting.Persistence;
using Karamchari.Forecasting.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Testcontainers.MsSql;
using Xunit;

namespace Karamchari.Forecasting.IntegrationTests.Infrastructure;

/// <summary>
/// Shared SQL Server container for workforce planning integration tests.
/// Lifecycle: started once per collection, torn down after all tests complete.
///
/// Schema strategy: EnsureCreated() against the literal "__tenant__" schema.
/// No TenantSchemaCommandInterceptor — tables remain in __tenant__ for the test run.
/// This avoids the full tenant provisioning stack while still exercising real SQL queries.
/// </summary>
public sealed class ForecastingSqlServerFixture : IAsyncLifetime
{
    private static readonly string TenantId = "wfp-integration-test";

    private readonly MsSqlContainer _container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Pooling = false,
            TrustServerCertificate = true,
        };
        _connectionString = builder.ConnectionString;

        // Create the __tenant__ schema that all WFP tables are mapped to in OnModelCreating.
        // EnsureCreated() generates CREATE TABLE statements using the literal schema name
        // from the EF model — so the schema must exist first.
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{KaramchariDbContext.PlaceholderSchema}')
                EXEC(N'CREATE SCHEMA [{KaramchariDbContext.PlaceholderSchema}]');
            """;
        await cmd.ExecuteNonQueryAsync();

        // Build the full WFP schema from the EF model (bypasses migrations — equivalent for tests).
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>Creates a ForecastingDbContext wired to the SQL Server container.</summary>
    public ForecastingDbContext CreateDbContext()
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns(TenantId);
        tenantProvider.GetTenant().Returns(new TenantExecutionEnvelope(
            TenantId, "corr", "req", ExecutionSource.Test, TenantSource.JwtClaim));

        var options = new DbContextOptionsBuilder<ForecastingDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        return new ForecastingDbContext(options, tenantProvider);
    }

    /// <summary>
    /// Builds a fully wired WorkforcePlanningConsumer with real services backed by the container.
    /// The EnqueueRecomputeAsync and RefreshAttritionRateAsync calls run for real — their
    /// side-effects (WfpRecomputeEntry rows) can be verified or ignored in tests.
    /// </summary>
    public WorkforcePlanningConsumer CreateConsumer(ForecastingDbContext db)
    {
        var demandGen = new DemandForecastGenerator(db);
        var supplyCalc = new SupplyForecastCalculator(db);
        var scenarioEngine = new ScenarioEngine(db, NullLogger<ScenarioEngine>.Instance);
        var wfpService = new WorkforcePlanningService(
            db, demandGen, supplyCalc, scenarioEngine,
            NullLogger<WorkforcePlanningService>.Instance);

        return new WorkforcePlanningConsumer(
            db, wfpService, NullLogger<WorkforcePlanningConsumer>.Instance);
    }

    /// <summary>
    /// Builds a SupplyForecastCalculator wired to the container.
    /// Used directly in boundary predicate tests.
    /// </summary>
    public SupplyForecastCalculator CreateSupplyCalculator(ForecastingDbContext db)
        => new(db);

    /// <summary>
    /// Builds a WorkforcePlanningService wired to the container.
    /// Used in performance tests that need direct access to EnqueueRecomputeAsync,
    /// RecomputeForTenantAsync, and GetAllTenantIdsAsync without going through the consumer.
    /// </summary>
    public WorkforcePlanningService CreateService(ForecastingDbContext db)
    {
        var demandGen = new DemandForecastGenerator(db);
        var supplyCalc = new SupplyForecastCalculator(db);
        var scenarioEngine = new ScenarioEngine(db, NullLogger<ScenarioEngine>.Instance);
        return new WorkforcePlanningService(
            db, demandGen, supplyCalc, scenarioEngine,
            NullLogger<WorkforcePlanningService>.Instance);
    }
}

[CollectionDefinition(nameof(ForecastingIntegrationCollection))]
public sealed class ForecastingIntegrationCollection : ICollectionFixture<ForecastingSqlServerFixture>;
