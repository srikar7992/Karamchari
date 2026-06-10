// -----------------------------------------------------------------------
// <copyright file="RabbitMqBrokerFixture.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using Karamchari.Core.DependencyInjection;
using Karamchari.Core.Messaging.Tenant;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Karamchari.Core.Persistence.Interceptors;
using Karamchari.DataMigration.Consumers;
using Karamchari.DataMigration.Contracts.Events;
using Karamchari.DataMigration.Features.Employees;
using Karamchari.DataMigration.Features.HistoricalPayroll;
using Karamchari.DataMigration.Features.LeaveBalance;
using Karamchari.DataMigration.Features.SalaryComponent;
using Karamchari.DataMigration.Features.SalaryRevision;
using Karamchari.DataMigration.Persistence;
using Karamchari.DataMigration.Services;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Karamchari.DataMigration.Tests.Integration.Fixtures;

/// <summary>
/// Shared fixture that provisions:
///   • A SQL Server container for DataMigrationDbContext
///   • A RabbitMQ container for the full broker async chain
///
/// The consumer service collection mirrors the worker topology:
///   AddKaramchariCore → DataMigrationDbContext → pipelines → MassTransit (RabbitMQ + consumer)
///
/// IReferenceResolver is substituted with a mock that returns pre-seeded IDs for the
/// standard broker-chain test employee numbers (BRKR001, BRKR002).
/// </summary>
public sealed class RabbitMqBrokerFixture : IAsyncLifetime
{
    // ── Containers ──────────────────────────────────────────────────────────
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();
    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:3.13-management-alpine")
        .Build();

    // ── Public state ────────────────────────────────────────────────────────
    public string SqlConnectionString { get; private set; } = string.Empty;

    public static readonly TenantExecutionEnvelope TenantDev =
        new("dev", "corr-broker", "req-broker", ExecutionSource.Test, TenantSource.JwtClaim);

    // ── Consumer host (holds the real consumer connected to RabbitMQ) ───────
    private IHost? _consumerHost;

    /// <summary>The publish endpoint backed by the real RabbitMQ broker.</summary>
    public IPublishEndpoint PublishEndpoint => _consumerHost!.Services.GetRequiredService<IPublishEndpoint>();

    /// <summary>The TenantExecutionContextAccessor for establishing context before publishing.</summary>
    public TenantExecutionContextAccessor ContextAccessor =>
        _consumerHost!.Services.GetRequiredService<TenantExecutionContextAccessor>();

    // ── IAsyncLifetime ───────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_sql.StartAsync(), _rabbit.StartAsync());

        var sqlBuilder = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            Pooling = false,
            TrustServerCertificate = true
        };
        SqlConnectionString = sqlBuilder.ConnectionString;

        await ProvisionSchemasAsync();
        await BuildAndStartConsumerHostAsync();
    }

    public async Task DisposeAsync()
    {
        if (_consumerHost is not null)
        {
            await _consumerHost.StopAsync(TimeSpan.FromSeconds(5));
            _consumerHost.Dispose();
        }
        await Task.WhenAll(_sql.DisposeAsync().AsTask(), _rabbit.DisposeAsync().AsTask());
    }

    // ── Public helpers ───────────────────────────────────────────────────────

    /// <summary>Creates a fresh DataMigrationDbContext scoped to tenant_dev.</summary>
    public DataMigrationDbContext CreateDbContext()
    {
        var tenantProvider = new ImportIntegrationFixture.FixedTenantProvider(TenantDev);
        var options = new DbContextOptionsBuilder<DataMigrationDbContext>()
            .UseSqlServer(SqlConnectionString)
            .AddInterceptors(
                new RlsSessionContextInterceptor(tenantProvider, NullLogger<RlsSessionContextInterceptor>.Instance),
                new TenantSchemaCommandInterceptor(tenantProvider, NullLogger<TenantSchemaCommandInterceptor>.Instance),
                new TenantStampingInterceptor(tenantProvider, NullLogger<TenantStampingInterceptor>.Instance))
            .Options;
        return new DataMigrationDbContext(options, tenantProvider);
    }

    /// <summary>
    /// Saves a file to the shared InMemoryFileStore and returns its ID.
    /// The consumer's IImportFileStore is the same instance, so it can retrieve it.
    /// </summary>
    public async Task<string> SaveFileAsync(string csv, string fileName = "import.csv")
    {
        var store = _consumerHost!.Services.GetRequiredService<IImportFileStore>();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        return await store.SaveFileAsync(stream, fileName);
    }

    /// <summary>
    /// Polls the database until the import job reaches a terminal state or the timeout
    /// elapses. Returns the final status.
    /// </summary>
    public async Task<Karamchari.DataMigration.Domain.Importing.ImportJobStatus> WaitForTerminalStatusAsync(
        Guid jobId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        var terminalStatuses = new HashSet<Karamchari.DataMigration.Domain.Importing.ImportJobStatus>
        {
            Karamchari.DataMigration.Domain.Importing.ImportJobStatus.Completed,
            Karamchari.DataMigration.Domain.Importing.ImportJobStatus.CompletedWithErrors,
            Karamchari.DataMigration.Domain.Importing.ImportJobStatus.Failed,
            Karamchari.DataMigration.Domain.Importing.ImportJobStatus.Cancelled,
            Karamchari.DataMigration.Domain.Importing.ImportJobStatus.RolledBack,
        };

        while (DateTime.UtcNow < deadline)
        {
            await using var db = CreateDbContext();
            var job = await db.ImportJobs
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job is not null && terminalStatuses.Contains(job.Status))
                return job.Status;

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"Import job {jobId} did not reach a terminal state within {timeout?.TotalSeconds ?? 30}s.");
    }

    // ── Internal: consumer host setup ────────────────────────────────────────

    private async Task BuildAndStartConsumerHostAsync()
    {
        // GetConnectionString() returns amqp://guest:guest@localhost:PORT/ (includes credentials)
        var rabbitMqUri = _rabbit.GetConnectionString();

        // Build minimal in-memory configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:ExecutionContextValidationMode"] = "AuditOnly",
                // ExecutionContextSigner reads a signing key from config
                ["Messaging:ContextSigningKey"] = "broker-chain-test-signing-key-xxxxx",
            })
            .Build();

        // Build a mock IReferenceResolver that resolves test employee numbers to known GUIDs
        var employeeId1 = Guid.NewGuid();
        var employeeId2 = Guid.NewGuid();
        var resolver = Substitute.For<IReferenceResolver>();
        resolver.ResolveEmployeeIdAsync("BRKR001", Arg.Any<CancellationToken>()).Returns(employeeId1);
        resolver.ResolveEmployeeIdAsync("BRKR002", Arg.Any<CancellationToken>()).Returns(employeeId2);
        resolver.ResolveEmployeeNameAsync(employeeId1, Arg.Any<CancellationToken>()).Returns("Alice Smith");
        resolver.ResolveEmployeeNameAsync(employeeId2, Arg.Any<CancellationToken>()).Returns("Bob Jones");

        var fileStore = new BrokerInMemoryFileStore();

        var services = new ServiceCollection();

        // Core infrastructure (replay protection, tenant metrics, execution context signer, etc.)
        services.AddKaramchariCore(configuration);

        // Replace HttpTenantProvider with a fixed test provider — the broker tests do
        // not have an HTTP context, so ITenantProvider must not depend on IHttpContextAccessor.
        services.RemoveAll<ITenantProvider>();
        services.AddSingleton<ITenantProvider>(
            new ImportIntegrationFixture.FixedTenantProvider(TenantDev));

        // DataMigrationDbContext — real SQL Server, full interceptors via AddKaramchariInterceptors
        services.AddDbContext<DataMigrationDbContext>((sp, opts) =>
        {
            opts.UseSqlServer(SqlConnectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "migration"));
            opts.AddKaramchariInterceptors(sp);
        });

        // File store — shared instance so test can pre-load files
        services.AddSingleton<IImportFileStore>(fileStore);

        // File parser
        services.AddScoped<IImportFileParser, CsvImportFileParser>();

        // Reference resolver — mock for cross-context lookups
        services.AddScoped<IReferenceResolver>(_ => resolver);

        // All 5 import pipelines
        RegisterImportPipelines(services);

        // MassTransit with real RabbitMQ + ImportJobQueuedConsumer
        services.AddSingleton(typeof(TenantPublishFilter<>));
        services.AddSingleton(typeof(TenantSendFilter<>));
        services.AddMassTransit(x =>
        {
            x.AddConsumer<ImportJobQueuedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqUri);

                cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
                cfg.UsePublishFilter(typeof(TenantPublishFilter<>), context);
                cfg.UsePublishFilter<TenantPublishFilter>(context);
                cfg.UseSendFilter(typeof(TenantSendFilter<>), context);

                // Single immediate retry — keeps tests fast
                cfg.UseMessageRetry(r => r.Immediate(1));

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

        _consumerHost = Host.CreateDefaultBuilder()
            .ConfigureServices((_, svc) =>
            {
                foreach (var descriptor in services)
                    svc.Add(descriptor);
            })
            .Build();

        await _consumerHost.StartAsync();

        // Give the consumer a moment to establish its RabbitMQ connection
        await Task.Delay(1000);
    }

    private static void RegisterImportPipelines(IServiceCollection services)
    {
        // Employee
        services.AddScoped<IImportMapper<EmployeeImportRecord>, EmployeeImportMapper>();
        services.AddScoped<IImportValidator<EmployeeImportRecord>, EmployeeImportValidator>();
        services.AddScoped<IImportProcessor<EmployeeImportRecord>, EmployeeImportProcessor>();
        services.AddScoped<IImportPipeline, ImportPipeline<EmployeeImportRecord>>();

        // Leave Balance — processor needs TimeAttendanceDbContext; use no-op stub
        services.AddScoped<IImportMapper<LeaveBalanceImportRecord>, LeaveBalanceImportMapper>();
        services.AddScoped<IImportValidator<LeaveBalanceImportRecord>, LeaveBalanceImportValidator>();
        services.AddScoped<IImportProcessor<LeaveBalanceImportRecord>>(_ =>
            new NoOpImportProcessor<LeaveBalanceImportRecord>("LeaveBalanceImport"));
        services.AddScoped<IImportPipeline, ImportPipeline<LeaveBalanceImportRecord>>();

        // Salary Component — processor writes to HistoricalSalaryRecords (DataMigrationDbContext); use no-op stub
        services.AddScoped<IImportMapper<SalaryComponentImportRecord>, SalaryComponentImportMapper>();
        services.AddScoped<IImportValidator<SalaryComponentImportRecord>, SalaryComponentImportValidator>();
        services.AddScoped<IImportProcessor<SalaryComponentImportRecord>>(_ =>
            new NoOpImportProcessor<SalaryComponentImportRecord>("SalaryComponentImport"));
        services.AddScoped<IImportPipeline, ImportPipeline<SalaryComponentImportRecord>>();

        // Salary Revision — processor needs ITenantProvider + PayrollDbContext
        // For broker chain tests, SalaryRevision processor is tested separately in unit tests.
        // Register with a no-op processor to keep the pipeline registry complete.
        services.AddScoped<IImportMapper<SalaryRevisionImportRecord>, SalaryRevisionImportMapper>();
        services.AddScoped<IImportValidator<SalaryRevisionImportRecord>, SalaryRevisionImportValidator>();
        services.AddScoped<IImportProcessor<SalaryRevisionImportRecord>>(_ =>
            new NoOpImportProcessor<SalaryRevisionImportRecord>("SalaryRevisionImport"));
        services.AddScoped<IImportPipeline, ImportPipeline<SalaryRevisionImportRecord>>();

        // Historical Payroll — processor needs DataMigrationDbContext injected separately
        services.AddScoped<IImportMapper<HistoricalPayrollImportRecord>, HistoricalPayrollImportMapper>();
        services.AddScoped<IImportValidator<HistoricalPayrollImportRecord>, HistoricalPayrollImportValidator>();
        services.AddScoped<IImportProcessor<HistoricalPayrollImportRecord>>(sp =>
            new HistoricalPayrollImportProcessor(sp.GetRequiredService<DataMigrationDbContext>()));
        services.AddScoped<IImportPipeline, ImportPipeline<HistoricalPayrollImportRecord>>();
    }

    // ── Schema provisioning ──────────────────────────────────────────────────

    private async Task ProvisionSchemasAsync()
    {
        foreach (var schema in new[] { "migration", "tenant_dev" })
        {
            await ExecuteSqlAsync($"""
                IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{schema}')
                BEGIN EXEC(N'CREATE SCHEMA [{schema}] AUTHORIZATION [dbo];'); END
                """);
        }

        var tenantProvider = new ImportIntegrationFixture.FixedTenantProvider(TenantDev);
        var options = new DbContextOptionsBuilder<DataMigrationDbContext>()
            .UseSqlServer(SqlConnectionString)
            .Options;
        await using var db = new DataMigrationDbContext(options, tenantProvider);

        var rawDdl = db.Database.GenerateCreateScript();
        var script = rawDdl
            .Replace("[__tenant__]", "[tenant_dev]", StringComparison.OrdinalIgnoreCase)
            .Replace("__tenant__", "tenant_dev", StringComparison.OrdinalIgnoreCase);

        var lines = script.Split('\n')
            .Where(l => !l.TrimStart().StartsWith("CREATE SCHEMA", StringComparison.OrdinalIgnoreCase));
        await ExecuteSqlBatchAsync(string.Join('\n', lines));
    }

    private async Task ExecuteSqlAsync(string sql)
    {
        await using var conn = new SqlConnection(SqlConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task ExecuteSqlBatchAsync(string script)
    {
        await using var conn = new SqlConnection(SqlConnectionString);
        await conn.OpenAsync();
        var batch = new StringBuilder();
        using var reader = new StringReader(script);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                if (batch.Length > 0)
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = batch.ToString();
                    await cmd.ExecuteNonQueryAsync();
                    batch.Clear();
                }
            }
            else { batch.AppendLine(line); }
        }
        if (batch.Length > 0)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = batch.ToString();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

/// <summary>
/// In-memory file store used by the broker chain fixture. A single instance is
/// shared between the test (which writes files) and the consumer (which reads them).
/// </summary>
internal sealed class BrokerInMemoryFileStore : IImportFileStore
{
    private readonly Dictionary<string, byte[]> _files = new();

    public async Task<string> SaveFileAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N") + Path.GetExtension(fileName);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        _files[id] = ms.ToArray();
        return id;
    }

    public Task<Stream> GetFileAsync(string fileId, CancellationToken ct = default)
    {
        if (!_files.TryGetValue(fileId, out var bytes))
            throw new FileNotFoundException($"File '{fileId}' not found in broker store.");
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task DeleteFileAsync(string fileId, CancellationToken ct = default)
    {
        _files.Remove(fileId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// A no-op processor stub for import types whose real processor has cross-context
/// dependencies not wired in the broker fixture (e.g. SalaryRevision needs PayrollDbContext).
/// </summary>
internal sealed class NoOpImportProcessor<T> : IImportProcessor<T>
{
    public NoOpImportProcessor(string importType) => ImportType = importType;
    public string ImportType { get; }
    public Task ProcessBatchAsync(IEnumerable<T> items, CancellationToken ct = default)
        => Task.CompletedTask;
}

[CollectionDefinition(nameof(RabbitMqBrokerCollection))]
public sealed class RabbitMqBrokerCollection : ICollectionFixture<RabbitMqBrokerFixture>;
