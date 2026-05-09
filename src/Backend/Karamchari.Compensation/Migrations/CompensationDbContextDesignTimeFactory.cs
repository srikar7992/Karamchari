using Karamchari.Compensation.Persistence;
using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Karamchari.Compensation.Migrations;

internal sealed class CompensationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CompensationDbContext>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CompensationDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Karamchari.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var connectionString = config.GetConnectionString("KaramchariDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:KaramchariDb not found in Karamchari.Api/appsettings.Development.json.");

        var options = new DbContextOptionsBuilder<CompensationDbContext>()
            .UseSqlServer(connectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_Compensation"))
            .Options;

        return new CompensationDbContext(options, DesignTimeTenantProvider.Instance);
    }

    private sealed class DesignTimeTenantProvider : ITenantProvider
    {
        /// <summary>
        /// Provides required documentation for this member.
        /// </summary>
        public static readonly DesignTimeTenantProvider Instance = new();
        private static readonly TenantContext DesignTimeTenant =
            new("design_time", TenantSource.Provisioning);

        /// <summary>
        /// Provides required documentation for this member.
        /// </summary>
        public TenantContext GetTenant() => DesignTimeTenant;
        /// <summary>
        /// Provides required documentation for this member.
        /// </summary>
        public bool TryGetTenant(out TenantContext? tenant) { tenant = DesignTimeTenant; return true; }
    }
}
