using Karamchari.Core.Multitenancy;
using Karamchari.Onboarding.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Karamchari.Onboarding.Migrations;

internal sealed class OnboardingDbContextDesignTimeFactory : IDesignTimeDbContextFactory<OnboardingDbContext>
{
    public OnboardingDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Karamchari.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var connectionString = config.GetConnectionString("KaramchariDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:KaramchariDb not found in Karamchari.Api/appsettings.Development.json.");

        var options = new DbContextOptionsBuilder<OnboardingDbContext>()
            .UseSqlServer(connectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_Onboarding"))
            .Options;

        return new OnboardingDbContext(options, DesignTimeTenantProvider.Instance);
    }

    private sealed class DesignTimeTenantProvider : ITenantProvider
    {
        public static readonly DesignTimeTenantProvider Instance = new();
        private static readonly TenantExecutionEnvelope DesignTimeTenant =
            new("design_time", "design_time_correlation", "design_time_request", ExecutionSource.Migration, TenantSource.Provisioning);

        public string GetCurrentTenantId() => DesignTimeTenant.TenantId;

        public bool TryGetCurrentTenantId([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? tenantId)
        {
            tenantId = DesignTimeTenant.TenantId;
            return true;
        }

        public TenantExecutionEnvelope GetTenant() => DesignTimeTenant;
        public bool TryGetTenant(out TenantExecutionEnvelope? tenant) { tenant = DesignTimeTenant; return true; }
    }
}
