using System.Diagnostics;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence.Interceptors;
using Karamchari.DataMigration.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.MsSql;
using Xunit;

namespace Karamchari.DataMigration.Tests.Integration.Fixtures;

/// <summary>
/// Shared SQL Server fixture for DataMigration integration tests.
/// Provisions the migration schema (no RLS — DataMigration tables are tenant-filtered via EF only).
/// </summary>
public sealed class ImportIntegrationFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public static readonly TenantExecutionEnvelope TenantDev =
        new("dev", "corr-dev", "req-dev", ExecutionSource.Test, TenantSource.JwtClaim);

    public static readonly TenantExecutionEnvelope TenantAcme =
        new("acme", "corr-acme", "req-acme", ExecutionSource.Test, TenantSource.JwtClaim);

    public static readonly TenantExecutionEnvelope TenantContoso =
        new("contoso", "corr-contoso", "req-contoso", ExecutionSource.Test, TenantSource.JwtClaim);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Pooling = false,
            TrustServerCertificate = true,
        };
        ConnectionString = builder.ConnectionString;

        // 1. Pre-create all tenant schemas — the TenantSchemaCommandInterceptor rewrites
        //    [__tenant__] but does not CREATE the schema automatically.
        await CreateSchemasAsync();

        // 2. Generate the DDL script from the EF model (uses placeholder [__tenant__])
        //    and execute it for each tenant, substituting the real schema name.
        //    We do NOT use EnsureCreatedAsync() because its "database already exists" check
        //    is global — after the first tenant creates tables, subsequent tenants are skipped.
        //    We strip CREATE SCHEMA lines since we already created schemas in step 1.
        await using var ddlDb = CreateDbContextForDdl();
        var rawDdl = ddlDb.Database.GenerateCreateScript();

        foreach (var tenant in new[] { TenantDev, TenantAcme, TenantContoso })
        {
            var tenantScript = rawDdl
                .Replace("[__tenant__]", $"[{tenant.SchemaName}]", StringComparison.OrdinalIgnoreCase)
                .Replace("__tenant__", tenant.SchemaName, StringComparison.OrdinalIgnoreCase);

            // Remove CREATE SCHEMA lines — schemas were pre-created above.
            var lines = tenantScript.Split('\n')
                .Where(l => !l.TrimStart().StartsWith("CREATE SCHEMA", StringComparison.OrdinalIgnoreCase));
            await ExecuteSqlBatchAsync(string.Join('\n', lines));
        }
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public DataMigrationDbContext CreateDbContext(TenantExecutionEnvelope tenant)
    {
        var tenantProvider = new FixedTenantProvider(tenant);
        var options = new DbContextOptionsBuilder<DataMigrationDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(
                new RlsSessionContextInterceptor(tenantProvider, NullLogger<RlsSessionContextInterceptor>.Instance),
                new TenantSchemaCommandInterceptor(tenantProvider, NullLogger<TenantSchemaCommandInterceptor>.Instance),
                new TenantStampingInterceptor(tenantProvider, NullLogger<TenantStampingInterceptor>.Instance))
            .Options;
        return new DataMigrationDbContext(options, tenantProvider);
    }

    /// <summary>
    /// Creates a DbContext purely for DDL generation — no interceptors, so the generated
    /// script retains the [__tenant__] placeholder that we substitute per-tenant.
    /// </summary>
    private DataMigrationDbContext CreateDbContextForDdl()
    {
        var tenantProvider = new FixedTenantProvider(TenantDev);
        var options = new DbContextOptionsBuilder<DataMigrationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new DataMigrationDbContext(options, tenantProvider);
    }

    private async Task CreateSchemasAsync()
    {
        foreach (var schemaName in new[] { "migration", "tenant_dev", "tenant_acme", "tenant_contoso" })
        {
            await ExecuteSqlAsync($"""
                IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{schemaName}')
                BEGIN
                    EXEC(N'CREATE SCHEMA [{schemaName}] AUTHORIZATION [dbo];');
                END
                """);
        }
    }

    /// <summary>
    /// Splits a SQL script on GO statements and executes each batch individually.
    /// </summary>
    public async Task ExecuteSqlBatchAsync(string script)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        var batch = new System.Text.StringBuilder();
        using var reader = new System.IO.StringReader(script);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                if (batch.Length > 0)
                {
                    await using var cmd = connection.CreateCommand();
                    cmd.CommandText = batch.ToString();
                    await cmd.ExecuteNonQueryAsync();
                    batch.Clear();
                }
            }
            else
            {
                batch.AppendLine(line);
            }
        }

        if (batch.Length > 0)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = batch.ToString();
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task ExecuteSqlAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    public sealed class FixedTenantProvider(TenantExecutionEnvelope envelope) : ITenantProvider
    {
        public string GetCurrentTenantId() => envelope.TenantId;

        public bool TryGetCurrentTenantId([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? tenantId)
        {
            tenantId = envelope.TenantId;
            return true;
        }

        public TenantExecutionEnvelope GetTenant() => envelope;

        public bool TryGetTenant(out TenantExecutionEnvelope? tenant)
        {
            tenant = envelope;
            return true;
        }
    }
}

[CollectionDefinition(nameof(ImportIntegrationCollection))]
public sealed class ImportIntegrationCollection : ICollectionFixture<ImportIntegrationFixture>;

/// <summary>
/// Skips the test unless Docker is available (matches existing platform convention).
/// </summary>
internal sealed class DockerRequiredFactAttribute : FactAttribute
{
    public DockerRequiredFactAttribute()
    {
        if (!IsCi() && !IsDockerAvailable())
            Skip = "Docker is required for SQL Server integration tests.";
    }

    private static bool IsCi() =>
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);

    private static bool IsDockerAvailable()
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });
            p?.WaitForExit(3000);
            return p?.ExitCode == 0;
        }
        catch { return false; }
    }
}
