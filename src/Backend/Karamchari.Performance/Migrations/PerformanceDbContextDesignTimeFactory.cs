using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Performance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Karamchari.Performance.Migrations;

/// <summary>
/// Design-time factory for <see cref="PerformanceDbContext"/>.
/// Used exclusively by EF Core CLI tools (dotnet ef) — never resolved at runtime.
/// </summary>
/// <remarks>
/// After running dotnet ef migrations add, open the generated migration file
/// and replace every [__tenant__] with [dbo].
///
/// Commands (from src/Backend/):
///   dotnet ef migrations add &lt;Name&gt; --project Karamchari.Performance --startup-project Karamchari.Api
///   # Edit: replace [__tenant__] → [dbo] in the generated migration
///   dotnet ef database update          --project Karamchari.Performance --startup-project Karamchari.Api
/// </remarks>
public sealed class PerformanceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PerformanceDbContext>
{
    public PerformanceDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Karamchari.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var connectionString = config.GetConnectionString("KaramchariDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:KaramchariDb not found in Karamchari.Api/appsettings.Development.json.");

        var optionsBuilder = new DbContextOptionsBuilder<PerformanceDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new PerformanceDbContext(optionsBuilder.Options, DesignTimeTenantProvider.Instance);
    }

    private sealed class DesignTimeTenantProvider : ITenantProvider
    {
        public static readonly DesignTimeTenantProvider Instance = new();
        private static readonly TenantContext DesignTimeTenant =
            new("design_time", TenantSource.Provisioning);

        public TenantContext GetTenant() => DesignTimeTenant;

        public bool TryGetTenant(out TenantContext? tenant)
        {
            tenant = DesignTimeTenant;
            return true;
        }
    }
}
