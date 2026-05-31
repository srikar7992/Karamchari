using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Karamchari.Core.Persistence;
using Karamchari.Core.Persistence.Interceptors;
using Karamchari.Core.Persistence.Provisioning;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain;
using Karamchari.Core.Contracts.IntegrationEvents.V1;
using Karamchari.Core.Messaging.Tenant;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.MsSql;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Database;

[Collection(nameof(SqlServerEndToEndCollectionDefinition))]
public sealed class EndToEndIsolationCertificationTests(SqlServerEndToEndFixture fixture)
{
    [DockerRequiredFact]
    public async Task MultiTenantAsyncFlow_MustMaintainStrictDatabaseIsolation()
    {
        // 1. Arrange
        var tenants = new[] { "acme", "dev", "contoso" };
        var messagesPerTenant = 10;
        var totalMessages = tenants.Length * messagesPerTenant;

        foreach (var tenantId in tenants)
        {
            await fixture.ProvisionTenantSchemaAsync(tenantId);
        }

        var config = new ConfigurationBuilder().Build();

        // Setup MassTransit Harness with real DB
        await using var provider = new ServiceCollection()
            .AddLogging(l => l.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<IConfiguration>(config)
            .AddSingleton<ITenantProvider, AmbientTenantProvider>()
            .AddSingleton(new ExecutionContextSigner("karamchari-internal-platform-secret-2026"))
            .AddSingleton<ReplayProtectionService>()
            .AddSingleton<Karamchari.Core.Observability.Tenant.TenantMetricsCollector>()
            .AddSingleton<Karamchari.Core.Observability.Tenant.TenantActivitySource>()
            .AddSingleton(typeof(TenantConsumeFilter<>))
            .AddSingleton(typeof(TenantPublishFilter<>))
            .AddSingleton<TenantPublishFilter>()
            .AddDbContext<PayrollDbContext>(o => 
            {
                o.UseSqlServer(fixture.ConnectionString);
                o.AddInterceptors(new TenantSchemaCommandInterceptor(
                    new AmbientTenantProvider(),
                    NullLogger<TenantSchemaCommandInterceptor>.Instance));
            })
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<PayrollProfileConsumer>();

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
                    cfg.UsePublishFilter(typeof(TenantPublishFilter<>), context);
                    cfg.UsePublishFilter<TenantPublishFilter>(context);
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider();

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        // 2. Act - Concurrent Load
        var tasks = tenants.SelectMany(tenantId =>
            Enumerable.Range(0, messagesPerTenant).Select(async i =>
            {
                var correlationId = $"{tenantId}-{i}";
                var envelope = new TenantExecutionEnvelope(tenantId, correlationId, $"req-{i}", ExecutionSource.HttpRequest, TenantSource.JwtClaim);
                var context = new TenantExecutionContext(envelope);

                using (context.Establish())
                {
                    await harness.Bus.Publish(new EmployeeOnboardedIntegrationEvent(
                        Guid.NewGuid(),
                        tenantId,
                        $"EMP-{tenantId}-{i}",
                        $"Employee {tenantId} {i}",
                        $"{i}@{tenantId}.com",
                        DateOnly.FromDateTime(DateTime.Today)));
                }
            })).ToList();

        await Task.WhenAll(tasks);

        // Wait for all consumers to finish processing
        var timeout = TimeSpan.FromSeconds(30);
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            var count = harness.Consumed.Select<EmployeeOnboardedIntegrationEvent>().Count();
            if (count >= totalMessages) break;
            await Task.Delay(500);
        }

        // 3. Assert - Database Evidence Package D & F
        foreach (var tenantId in tenants)
        {
            var schema = $"tenant_{tenantId}";
            var count = await fixture.GetRowCountAsync(schema, "PayrollProfiles");
            count.Should().Be(messagesPerTenant, $"Tenant {tenantId} should have exactly {messagesPerTenant} rows in its own schema.");
        }

        var dboCount = await fixture.GetRowCountAsync("dbo", "PayrollProfiles");
        dboCount.Should().Be(0, "The dbo schema MUST remain empty. Any rows here prove an isolation failure (D1 root cause).");
    }

    private class PayrollProfileConsumer(PayrollDbContext dbContext) : IConsumer<EmployeeOnboardedIntegrationEvent>
    {
        public async Task Consume(ConsumeContext<EmployeeOnboardedIntegrationEvent> context)
        {
            var message = context.Message;
            var profile = PayrollProfile.CreateDraft(message.EmployeeId, message.LegalName);
            dbContext.PayrollProfiles.Add(profile);
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
    }

    private class AmbientTenantProvider : ITenantProvider
    {
        public string GetCurrentTenantId() => TenantExecutionContext.Current?.TenantId ?? "NULL";
        public bool TryGetCurrentTenantId([NotNullWhen(true)] out string? tenantId) 
        { 
            tenantId = TenantExecutionContext.Current?.TenantId; 
            return tenantId != null; 
        }
        public TenantExecutionEnvelope GetTenant() => TenantExecutionContext.Current?.Envelope ?? throw new InvalidOperationException("No tenant context");
        public bool TryGetTenant([NotNullWhen(true)] out TenantExecutionEnvelope? tenant)
        {
            tenant = TenantExecutionContext.Current?.Envelope;
            return tenant != null;
        }
    }
}

[CollectionDefinition(nameof(SqlServerEndToEndCollectionDefinition))]
public sealed class SqlServerEndToEndCollectionDefinition : ICollectionFixture<SqlServerEndToEndFixture>;

public sealed class SqlServerEndToEndFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        await CreateSharedTablesInDboAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private async Task CreateSharedTablesInDboAsync()
    {
        await ExecuteRawSqlAsync(
            """
            CREATE TABLE [dbo].[PayrollProfiles] (
                [Id] UNIQUEIDENTIFIER PRIMARY KEY,
                [TenantId] NVARCHAR(64) NOT NULL,
                [EmployeeId] UNIQUEIDENTIFIER NOT NULL,
                [EmployeeName] NVARCHAR(200) NOT NULL,
                [PayType] INT NOT NULL,
                [BaseSalary] DECIMAL(18,2) NOT NULL,
                [HourlyRate] DECIMAL(18,2) NOT NULL,
                [Currency] NVARCHAR(10) NOT NULL,
                [IsActive] BIT NOT NULL,
                [OptedForVoluntaryPF] BIT NOT NULL,
                [IsEsicLocked] BIT NOT NULL,
                [StateCode] NVARCHAR(10) NOT NULL,
                [TaxRegime] INT NOT NULL,
                [IsMetro] BIT NOT NULL,
                [AnnualCTC] DECIMAL(18,2) NOT NULL,
                [SalaryTemplateId] UNIQUEIDENTIFIER NOT NULL,
                [Pan] NVARCHAR(20) NOT NULL,
                [Uan] NVARCHAR(20) NOT NULL,
                [EsicNumber] NVARCHAR(20) NOT NULL
            );
            """);
    }

    public async Task ProvisionTenantSchemaAsync(string tenantId)
    {
        var schema = $"tenant_{tenantId}";
        await ExecuteRawSqlAsync(
            $"""
            IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{schema}')
            BEGIN
                EXEC(N'CREATE SCHEMA [{schema}] AUTHORIZATION [dbo];');
            END

            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PayrollProfiles' AND schema_id = SCHEMA_ID(N'{schema}'))
            BEGIN
                SELECT * INTO [{schema}].[PayrollProfiles] FROM [dbo].[PayrollProfiles] WHERE 1=0;
            END
            """);
    }

    public async Task<int> GetRowCountAsync(string schema, string table)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM [{schema}].[{table}]";
        var result = await command.ExecuteScalarAsync();
        return (int)(result ?? 0);
    }

    private async Task ExecuteRawSqlAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}

internal sealed class DockerRequiredFactAttribute : FactAttribute
{
    public DockerRequiredFactAttribute()
    {
        if (!IsDockerAvailable())
        {
            Skip = "Docker is required for End-to-End Isolation tests.";
        }
    }

    private static bool IsDockerAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                ArgumentList = { "info" },
                UseShellExecute = false,
                CreateNoWindow = true
            });
            return process is not null && process.WaitForExit(2000) && process.ExitCode == 0;
        }
        catch { return false; }
    }
}
