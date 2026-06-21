using Karamchari.Core.Multitenancy;
using Karamchari.Extensibility.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Diagnostics.CodeAnalysis;

namespace Karamchari.Extensibility.Migrations;

public sealed class ExtensibilityDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ExtensibilityDbContext>
{
    public ExtensibilityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ExtensibilityDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=Karamchari;Trusted_Connection=True;");
        return new ExtensibilityDbContext(optionsBuilder.Options, new DesignTimeTenantProvider());
    }

    private sealed class DesignTimeTenantProvider : ITenantProvider
    {
        public string GetCurrentTenantId() => "design-time";
        public bool TryGetCurrentTenantId([NotNullWhen(true)] out string? tenantId) { tenantId = "design-time"; return true; }
        public TenantExecutionEnvelope GetTenant() => throw new NotSupportedException();
        public bool TryGetTenant(out TenantExecutionEnvelope? envelope) { envelope = null; return false; }
        public void SetTenant(string tenantId) => throw new NotSupportedException();
    }
}
