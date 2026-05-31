using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.DataMigration.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Karamchari.DataMigration.Migrations;

/// <summary>
/// Design-time factory for <see cref="DataMigrationDbContext"/>.
/// </summary>
public sealed class DataMigrationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<DataMigrationDbContext>
{
    public DataMigrationDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Karamchari.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var connectionString = config.GetConnectionString("KaramchariDb")
            ?? throw new InvalidOperationException("Connection string 'KaramchariDb' is not configured.");

        var optionsBuilder = new DbContextOptionsBuilder<DataMigrationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new DataMigrationDbContext(optionsBuilder.Options, DesignTimeTenantProvider.Instance);
    }

    private sealed class DesignTimeTenantProvider : ITenantProvider
    {
        public static readonly DesignTimeTenantProvider Instance = new();
        private static readonly TenantExecutionEnvelope DesignTimeTenant =
            new("design_time", "dt_corr", "dt_req", ExecutionSource.Migration, TenantSource.Provisioning);

        public string GetCurrentTenantId() => DesignTimeTenant.TenantId;
        public bool TryGetCurrentTenantId([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? tenantId) { tenantId = DesignTimeTenant.TenantId; return true; }
        public TenantExecutionEnvelope GetTenant() => DesignTimeTenant;
        public bool TryGetTenant(out TenantExecutionEnvelope? tenant) { tenant = DesignTimeTenant; return true; }
    }
}
