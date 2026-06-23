// -----------------------------------------------------------------------
// <copyright file="RlsSqlServerIntegrationTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using System.Diagnostics;
using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Core.Persistence.Interceptors;
using Karamchari.Core.Persistence.Provisioning;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Testcontainers.MsSql;
using Xunit;

namespace Karamchari.Core.IntegrationTests.Persistence;

[Collection(nameof(SqlServerRlsCollection))]
public sealed class RlsSqlServerIntegrationTests(SqlServerRlsFixture fixture)
{
    private static readonly TenantExecutionEnvelope TenantA = new("contoso", "corr-a", "req-a", ExecutionSource.Test, TenantSource.JwtClaim);
    private static readonly TenantExecutionEnvelope TenantB = new("fabrikam", "corr-b", "req-b", ExecutionSource.Test, TenantSource.JwtClaim);

    [DockerRequiredFact]
    public async Task Query_UnderTenantSessionContext_ReturnsOnlyRowsOwnedByThatTenant()
    {
        await using var db = fixture.CreateDbContext(TenantA);

        var employees = await db.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.Name)
            .Select(employee => new { employee.TenantId, employee.Name })
            .ToListAsync();

        employees.Should().ContainSingle();
        employees[0].TenantId.Should().Be(TenantA.TenantId);
        employees[0].Name.Should().Be("Contoso Visible");
    }

    [DockerRequiredFact]
    public async Task Insert_WithAnotherTenantIdUnderCurrentSession_IsBlockedByRls()
    {
        await using var db = fixture.CreateDbContext(TenantA);

        var act = () => db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO [__tenant__].[Employees] ([Id], [TenantId], [Name])
            VALUES ({0}, {1}, {2});
            """,
            Guid.NewGuid(),
            TenantB.TenantId,
            "Cross Tenant Insert");

        await act.Should().ThrowAsync<Exception>()
            .Where(ex => IsRlsBlockPredicateFailure(ex));
    }

    [DockerRequiredFact]
    public async Task RawSql_BypassingSchemaRewriterAgainstAnotherTenantSchema_IsBlockedByRls()
    {
        await using var db = fixture.CreateDbContext(TenantA);
        var sql =
            $"INSERT INTO [{TenantB.SchemaName}].[Employees] ([Id], [TenantId], [Name]) VALUES ({{0}}, {{1}}, {{2}});";

        var act = () => db.Database.ExecuteSqlRawAsync(
            sql,
            Guid.NewGuid(),
            TenantB.TenantId,
            "Raw Bypass Insert");

        await act.Should().ThrowAsync<Exception>()
            .Where(ex => IsRlsBlockPredicateFailure(ex));
    }

    private static bool IsRlsBlockPredicateFailure(Exception ex)
    {
        var current = ex;
        while (current is not null)
        {
            if (current.Message.Contains("block predicate", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("security policy", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}

[CollectionDefinition(nameof(SqlServerRlsCollection))]
public sealed class SqlServerRlsCollection : ICollectionFixture<SqlServerRlsFixture>;

public sealed class SqlServerRlsFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container;

    public SqlServerRlsFixture()
    {
        // Testcontainers MatchImage uses [GeneratedRegex] which inherits the AppDomain
        // REGEX_DEFAULT_MATCH_TIMEOUT. Under parallel test load the default can be a
        // finite value and the image-name regex times out before completing. Force infinite.
        AppContext.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", Timeout.InfiniteTimeSpan);
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    }

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

        await ApplyRlsBootstrapAsync();
        await ProvisionTenantAsync(new TenantExecutionEnvelope("contoso", "test-corr", "test-req", ExecutionSource.Test, TenantSource.JwtClaim));
        await ProvisionTenantAsync(new TenantExecutionEnvelope("fabrikam", "test-corr", "test-req", ExecutionSource.Test, TenantSource.JwtClaim));
        await SeedRowsBeforePoliciesAsync();
        await ApplyTenantPolicyAsync(new TenantExecutionEnvelope("contoso", "test-corr", "test-req", ExecutionSource.Test, TenantSource.JwtClaim));
        await ApplyTenantPolicyAsync(new TenantExecutionEnvelope("fabrikam", "test-corr", "test-req", ExecutionSource.Test, TenantSource.JwtClaim));
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public RlsTestDbContext CreateDbContext(TenantExecutionEnvelope tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        var tenantProvider = new FixedTenantProvider(tenant);
        var options = new DbContextOptionsBuilder<RlsTestDbContext>()
            .UseSqlServer(_connectionString)
            .AddInterceptors(
                new TenantSchemaCommandInterceptor(
                    tenantProvider,
                    NullLogger<TenantSchemaCommandInterceptor>.Instance),
                new RlsSessionContextInterceptor(
                    tenantProvider,
                    NullLogger<RlsSessionContextInterceptor>.Instance))
            .Options;

        return new RlsTestDbContext(options, tenantProvider);
    }

    private async Task ApplyRlsBootstrapAsync()
    {
        var generator = CreateRlsScriptGenerator();

        foreach (var script in RlsScriptGenerator.BuildBootstrapScripts())
        {
            await ExecuteScriptAsync(script);
        }
    }

    private async Task ProvisionTenantAsync(TenantExecutionEnvelope tenant)
    {
        await ExecuteScriptAsync(
            $$"""
            IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{{tenant.SchemaName}}')
            BEGIN
                EXEC(N'CREATE SCHEMA [{{tenant.SchemaName}}] AUTHORIZATION [dbo];');
            END
            GO

            CREATE TABLE [{{tenant.SchemaName}}].[Employees]
            (
                [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_{{tenant.SchemaName}}_Employees] PRIMARY KEY,
                [TenantId] NVARCHAR(64) NOT NULL,
                [Name] NVARCHAR(200) NOT NULL
            );
            GO
            """);
    }

    private async Task SeedRowsBeforePoliciesAsync()
    {
        await ExecuteScriptAsync(
            $$"""
            INSERT INTO [tenant_contoso].[Employees] ([Id], [TenantId], [Name])
            VALUES
                (NEWID(), N'contoso', N'Contoso Visible'),
                (NEWID(), N'fabrikam', N'Contoso Schema Contaminant');

            INSERT INTO [tenant_fabrikam].[Employees] ([Id], [TenantId], [Name])
            VALUES
                (NEWID(), N'fabrikam', N'Fabrikam Visible'),
                (NEWID(), N'contoso', N'Fabrikam Schema Contaminant');
            GO
            """);
    }

    private async Task ApplyTenantPolicyAsync(TenantExecutionEnvelope tenant)
    {
        var generator = CreateRlsScriptGenerator();
        await ExecuteScriptAsync(generator.BuildTenantPolicyScript(tenant));
    }

    private static RlsScriptGenerator CreateRlsScriptGenerator()
    {
        var discovery = Substitute.For<ITenantModelDiscoveryService>();
        discovery.Discover().Returns(new[]
        {
            new TenantRelationalArtifact
            {
                DbContextName = "Test",
                EntityName = "Employee",
                TableName = "Employees",
                Schema = "__tenant__",
                IsTenantScoped = true
            }
        });
        return new RlsScriptGenerator(discovery);
    }

    private async Task ExecuteScriptAsync(string script)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        foreach (var batch in SplitSqlBatches(script))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = batch;
            await command.ExecuteNonQueryAsync();
        }
    }

    private static IEnumerable<string> SplitSqlBatches(string script)
    {
        var batch = new List<string>();

        using var reader = new StringReader(script);
        while (reader.ReadLine() is { } line)
        {
            if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                if (batch.Count > 0)
                {
                    yield return string.Join(Environment.NewLine, batch);
                    batch.Clear();
                }

                continue;
            }

            batch.Add(line);
        }

        if (batch.Count > 0)
        {
            yield return string.Join(Environment.NewLine, batch);
        }
    }

    private sealed class FixedTenantProvider(TenantExecutionEnvelope tenant) : ITenantProvider
    {
        public string GetCurrentTenantId() => tenant.TenantId;

        public bool TryGetCurrentTenantId([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? tenantId)
        {
            tenantId = tenant.TenantId;
            return true;
        }

        public TenantExecutionEnvelope GetTenant() => tenant;

        public bool TryGetTenant(out TenantExecutionEnvelope? resolvedTenant)
        {
            resolvedTenant = tenant;
            return true;
        }
    }
}

public sealed class RlsTestDbContext(DbContextOptions<RlsTestDbContext> options, ITenantProvider tenantProvider)
    : KaramchariDbContext(options, tenantProvider)
{
    public DbSet<RlsTestEmployee> Employees => Set<RlsTestEmployee>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<RlsTestEmployee>(builder =>
        {
            builder.ToTable("Employees");
            builder.HasKey(employee => employee.Id);
            builder.Property(employee => employee.TenantId).HasMaxLength(64).IsRequired();
            builder.Property(employee => employee.Name).HasMaxLength(200).IsRequired();
        });
    }
}

public sealed class RlsTestEmployee : ITenantOwned
{
    public Guid Id { get; private set; }

    public string TenantId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;
}

internal sealed class DockerRequiredFactAttribute : FactAttribute
{
    public DockerRequiredFactAttribute()
    {
        if (!IsContinuousIntegration() && !IsDockerAvailable())
        {
            Skip = "Docker is required for SQL Server RLS integration tests. Start Docker or run in CI.";
        }
    }

    private static bool IsContinuousIntegration()
    {
        return string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDockerAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                ArgumentList = { "info", "--format", "{{.ServerVersion}}" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            return process is not null && process.WaitForExit(5000) && process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
