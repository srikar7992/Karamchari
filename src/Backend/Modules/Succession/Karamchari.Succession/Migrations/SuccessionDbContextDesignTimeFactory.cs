using Karamchari.Core.Multitenancy;
using Karamchari.Succession.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Diagnostics.CodeAnalysis;

namespace Karamchari.Succession.Migrations;

public sealed class SuccessionDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SuccessionDbContext>
{
    public SuccessionDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SuccessionDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=Karamchari;Trusted_Connection=True;");
        return new SuccessionDbContext(optionsBuilder.Options, new DesignTimeTenantProvider());
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
