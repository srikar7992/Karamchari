// -----------------------------------------------------------------------
// <copyright file="ApiCertificationFactory.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Karamchari.Core.DependencyInjection;
using Karamchari.Core.Multitenancy;
using Karamchari.DataMigration.Persistence;
using Karamchari.DataMigration.Services;
using Karamchari.DataMigration.Tests.Integration.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;
using Xunit;

namespace Karamchari.DataMigration.Tests.ApiCertification;

/// <summary>
/// WebApplicationFactory that uses Testcontainers SQL Server for DataMigrationDbContext
/// and InMemory databases for all other module contexts. RabbitMQ is disabled so the
/// API uses the InMemory MassTransit transport.
/// </summary>
public sealed class ApiCertificationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    internal const string TestJwtSecret =
        "karamchari-api-cert-test-secret-minimum-32-bytes-xxxxx";
    internal const string TestIssuer = "https://karamchari.com";
    internal const string TestAudience = "https://karamchari.com/api";
    internal const string TestTenantId = "dev";

    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private string _connectionString = string.Empty;

    internal string ConnectionString => _connectionString;

    /// <summary>
    /// Directly advances an import job to the given status via raw SQL, bypassing the
    /// async worker pipeline. Required in tests where the MassTransit worker is disabled.
    /// Throws if no row is matched (indicates table/schema mismatch or wrong ID).
    /// </summary>
    internal async Task AdvanceJobStatusAsync(Guid jobId, int statusValue)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        // Use parameterized query to avoid GUID string-format issues
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"UPDATE [tenant_{TestTenantId}].[Migration_ImportJobs] SET [Status] = @status WHERE [Id] = @id";
        cmd.Parameters.AddWithValue("@status", statusValue);
        cmd.Parameters.AddWithValue("@id", jobId);
        var affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            throw new InvalidOperationException(
                $"AdvanceJobStatusAsync: 0 rows updated for Id={jobId} in [tenant_{TestTenantId}].[Migration_ImportJobs]. " +
                "Job may not have been committed or schema name is wrong.");
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        var builder = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            Pooling = false,
            TrustServerCertificate = true
        };
        _connectionString = builder.ConnectionString;

        await ProvisionDataMigrationSchemaAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Override configuration before services are registered — the connection string
        // is read at service registration time, so this must come first.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // DataMigrationDbContext uses this real SQL Server container
                ["ConnectionStrings:KaramchariDb"] = _connectionString,
                // Force InMemory MassTransit transport — no broker needed for API-level tests
                ["ConnectionStrings:RabbitMQ"] = string.Empty,
                // Known secret so tests can mint their own JWT tokens
                ["Jwt:Secret"] = TestJwtSecret,
                // Suppress cryptographic signature validation on test messages
                ["Messaging:ExecutionContextValidationMode"] = "AuditOnly",
            });
        });

        builder.ConfigureServices(services =>
        {
            // ── Replace all non-DataMigration DbContexts with InMemory ────────
            RemoveAndReplaceWithInMemory(services);

            // ── Re-wire DataMigrationDbContext to the test container ──────────
            // ConfigureAppConfiguration overrides run AFTER Program.cs service
            // registration in .NET 6+ minimal APIs. The captured closure in
            // AddKaramchariDataMigration already holds the appsettings value.
            // We must re-register here (ConfigureServices runs after Program.cs)
            // to guarantee the test-container connection string is actually used.
            services.RemoveAll<DbContextOptions<DataMigrationDbContext>>();
            services.RemoveAll<DataMigrationDbContext>();
            services.AddDbContext<DataMigrationDbContext>((sp, opts) =>
            {
                opts.UseSqlServer(_connectionString, sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "migration");
                    sql.EnableRetryOnFailure();
                });
                opts.AddKaramchariInterceptors(sp);
            });

            // ── Remove background hosted services that need real infrastructure ──
            services.RemoveAll<IHostedService>();

            // ── Replace file store with an in-memory implementation ──────────
            services.RemoveAll<IImportFileStore>();
            services.AddSingleton<IImportFileStore>(new CertificationInMemoryFileStore());
        });
    }

    // ── JWT helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a JWT for an Admin user in the test tenant. The Admin role bypasses all
    /// PermissionHandler requirement checks, so this token is valid for every endpoint.
    /// </summary>
    internal static string CreateAdminToken(string tenantId = TestTenantId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, $"admin@{tenantId}.local"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("tenant_id", tenantId),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("permission_version", "2026.05.30.2"),
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Schema provisioning ──────────────────────────────────────────────────

    private async Task ProvisionDataMigrationSchemaAsync()
    {
        // 1. Pre-create the schemas the EF model references
        foreach (var schema in new[] { "migration", "tenant_dev" })
        {
            await ExecuteSqlAsync($"""
                IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{schema}')
                BEGIN EXEC(N'CREATE SCHEMA [{schema}] AUTHORIZATION [dbo];'); END
                """);
        }

        // 2. Generate the DDL offline (no TenantSchemaCommandInterceptor active here,
        //    so [__tenant__] placeholder stays intact in the script) and execute it
        //    once for tenant_dev, substituting the schema name.
        var ddlOptions = new DbContextOptionsBuilder<DataMigrationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        var provider = new ImportIntegrationFixture.FixedTenantProvider(
            new TenantExecutionEnvelope("dev", "cert-init", "cert-init",
                ExecutionSource.Test, TenantSource.JwtClaim));
        await using var ddlDb = new DataMigrationDbContext(ddlOptions, provider);

        var rawDdl = ddlDb.Database.GenerateCreateScript();
        var tenantScript = rawDdl
            .Replace("[__tenant__]", "[tenant_dev]", StringComparison.OrdinalIgnoreCase)
            .Replace("__tenant__", "tenant_dev", StringComparison.OrdinalIgnoreCase);

        var filtered = tenantScript.Split('\n')
            .Where(l => !l.TrimStart().StartsWith("CREATE SCHEMA", StringComparison.OrdinalIgnoreCase));
        await ExecuteSqlBatchAsync(string.Join('\n', filtered));
    }

    // ── InMemory context replacement helpers ─────────────────────────────────

    private static void RemoveAndReplaceWithInMemory(IServiceCollection services)
    {
        // Each context gets its OWN isolated EF internal service provider via
        // UseInternalServiceProvider. Without this, MassTransit's AddEntityFrameworkOutbox<T>
        // registers IDbContextOptionsConfiguration<T> services that add SQL Server config at
        // runtime — combining with the InMemory replacement causes EF to see two providers.
        // Using per-context isolated providers prevents EF from looking at the app container.
        static void InMemory(DbContextOptionsBuilder o, string name)
        {
            var isolatedProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();
            o.UseInMemoryDatabase(name)
             .UseInternalServiceProvider(isolatedProvider);
        }

        var otherContexts = new[]
        {
            typeof(Karamchari.Identity.Infrastructure.Persistence.IdentityDbContext),
            typeof(Karamchari.HR.Persistence.HRDbContext),
            typeof(Karamchari.Payroll.Data.PayrollDbContext),
            typeof(Karamchari.TimeAttendance.Persistence.TimeAttendanceDbContext),
            typeof(Karamchari.PSA.Persistence.PSADbContext),
            typeof(Karamchari.Performance.Persistence.PerformanceDbContext),
            typeof(Karamchari.Notifications.Persistence.NotificationsDbContext),
            typeof(Karamchari.Compensation.Persistence.CompensationDbContext),
            typeof(Karamchari.Billing.Persistence.BillingDbContext),
            typeof(Karamchari.Forecasting.Persistence.ForecastingDbContext),
            typeof(Karamchari.Core.Persistence.CoreDbContext),
            typeof(Karamchari.Workflow.Persistence.WorkflowDbContext),
            typeof(Karamchari.Recruitment.Persistence.RecruitmentDbContext),
            typeof(Karamchari.Capability.Persistence.CapabilityDbContext),
            typeof(Karamchari.Intelligence.Persistence.IntelligenceDbContext),
            typeof(Karamchari.Governance.Persistence.GovernanceDbContext),
            typeof(Karamchari.FinancialOps.Persistence.FinancialOpsDbContext),
            typeof(Karamchari.Core.Messaging.Outbox.OutboxRelayDbContext),
        };

        foreach (var ctxType in otherContexts)
        {
            var optType = typeof(DbContextOptions<>).MakeGenericType(ctxType);
            services.RemoveAll(optType);
            services.RemoveAll(ctxType);
        }
        services.RemoveAll<DbContext>();
        services.RemoveAll<DbContextOptions<DbContext>>();

        services.AddDbContext<Karamchari.Identity.Infrastructure.Persistence.IdentityDbContext>(
            o => InMemory(o, "CertIdentity"), ServiceLifetime.Scoped, ServiceLifetime.Singleton);
        services.AddDbContextFactory<Karamchari.Identity.Infrastructure.Persistence.IdentityDbContext>(
            o => InMemory(o, "CertIdentity"), ServiceLifetime.Singleton);
        services.AddDbContext<Karamchari.HR.Persistence.HRDbContext>(o => InMemory(o, "CertHR"));
        services.AddDbContext<Karamchari.Payroll.Data.PayrollDbContext>(o => InMemory(o, "CertPayroll"));
        services.AddDbContext<Karamchari.TimeAttendance.Persistence.TimeAttendanceDbContext>(
            o => InMemory(o, "CertTA"));
        services.AddDbContext<Karamchari.PSA.Persistence.PSADbContext>(o => InMemory(o, "CertPSA"));
        services.AddDbContext<Karamchari.Performance.Persistence.PerformanceDbContext>(
            o => InMemory(o, "CertPerf"));
        services.AddDbContext<Karamchari.Notifications.Persistence.NotificationsDbContext>(
            o => InMemory(o, "CertNotif"));
        services.AddDbContext<Karamchari.Compensation.Persistence.CompensationDbContext>(
            o => InMemory(o, "CertComp"));
        services.AddDbContext<Karamchari.Billing.Persistence.BillingDbContext>(
            o => InMemory(o, "CertBilling"));
        services.AddDbContext<Karamchari.Forecasting.Persistence.ForecastingDbContext>(
            o => InMemory(o, "CertForecast"));
        services.AddDbContext<Karamchari.Core.Persistence.CoreDbContext>(o => InMemory(o, "CertCore"));
        services.AddDbContext<Karamchari.Workflow.Persistence.WorkflowDbContext>(
            o => InMemory(o, "CertWorkflow"));
        services.AddDbContext<Karamchari.Recruitment.Persistence.RecruitmentDbContext>(
            o => InMemory(o, "CertRecruit"));
        services.AddDbContext<Karamchari.Capability.Persistence.CapabilityDbContext>(
            o => InMemory(o, "CertCapability"));
        services.AddDbContext<Karamchari.Intelligence.Persistence.IntelligenceDbContext>(
            o => InMemory(o, "CertIntel"));
        services.AddDbContext<Karamchari.Governance.Persistence.GovernanceDbContext>(
            o => InMemory(o, "CertGov"));
        services.AddDbContext<Karamchari.FinancialOps.Persistence.FinancialOpsDbContext>(
            o => InMemory(o, "CertFinOps"));
        services.AddDbContext<Karamchari.Core.Messaging.Outbox.OutboxRelayDbContext>(
            o => InMemory(o, "CertOutbox"));

        services.AddScoped<DbContext>(sp =>
            sp.GetRequiredService<Karamchari.Payroll.Data.PayrollDbContext>());
    }

    // ── SQL execution helpers ────────────────────────────────────────────────

    private async Task ExecuteSqlAsync(string sql)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task ExecuteSqlBatchAsync(string script)
    {
        await using var conn = new SqlConnection(_connectionString);
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
            else
            {
                batch.AppendLine(line);
            }
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
/// An in-memory <see cref="IImportFileStore"/> for API certification tests.
/// Stores files by ID in a dictionary so the API upload and retrieve flows work
/// without touching the file system.
/// </summary>
internal sealed class CertificationInMemoryFileStore : IImportFileStore
{
    private readonly Dictionary<string, byte[]> _files = new();

    public async Task<string> SaveFileAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N") + System.IO.Path.GetExtension(fileName);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        _files[id] = ms.ToArray();
        return id;
    }

    public Task<Stream> GetFileAsync(string fileId, CancellationToken ct = default)
    {
        if (!_files.TryGetValue(fileId, out var bytes))
            throw new FileNotFoundException($"File '{fileId}' not found in certification store.");
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task DeleteFileAsync(string fileId, CancellationToken ct = default)
    {
        _files.Remove(fileId);
        return Task.CompletedTask;
    }
}

[CollectionDefinition(nameof(ApiCertificationCollection))]
public sealed class ApiCertificationCollection : ICollectionFixture<ApiCertificationFactory>;
