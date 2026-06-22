using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Karamchari.Intelligence.Migrations;

/// <summary>
/// Design-time factory used by EF Core tooling (dotnet ef migrations add) to
/// instantiate IntelligenceDbContext without a running host.
/// </summary>
internal sealed class IntelligenceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<IntelligenceDbContext>
{
    public IntelligenceDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<IntelligenceDbContext>();
        var connectionString = configuration.GetConnectionString("KaramchariDb")
            ?? "Server=(localdb)\\mssqllocaldb;Database=Karamchari_Design;Trusted_Connection=True;";

        optionsBuilder.UseSqlServer(connectionString);

        return new IntelligenceDbContext(optionsBuilder.Options, new DesignTimeTenantProvider());
    }

    private sealed class DesignTimeTenantProvider : ITenantProvider
    {
        public string GetCurrentTenantId() => "design-time";
        public bool TryGetCurrentTenantId(out string tenantId) { tenantId = "design-time"; return true; }
        public TenantExecutionEnvelope GetTenant() => throw new NotSupportedException();
        public bool TryGetTenant(out TenantExecutionEnvelope? envelope) { envelope = null; return false; }
        public void SetTenant(string tenantId) => throw new NotSupportedException();
    }
}
