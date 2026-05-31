namespace Karamchari.Payroll.Migrations;

using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Payroll.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Design-time factory for <see cref="PayrollDbContext"/>.
/// Used exclusively by the EF Core CLI tools (<c>dotnet ef</c>) â€” never resolved
/// at runtime by the application's DI container.
/// </summary>
/// <remarks>
/// <para>
/// <b>Migration workflow</b>
/// </para>
/// <para>
/// This project uses a Separated-Schema multi-tenancy model where the EF model bakes
/// <c>[__tenant__]</c> as a placeholder schema. The
/// <c>TenantSchemaCommandInterceptor</c> rewrites it to the active tenant's schema at
/// runtime, but EF's migration generator emits the literal placeholder.
/// </para>
/// <para>
/// After running <c>dotnet ef migrations add &lt;MigrationName&gt;</c>, open the
/// generated <c>*_&lt;MigrationName&gt;.cs</c> file and replace every occurrence of
/// <c>[__tenant__]</c> with <c>[dbo]</c>. The <c>dbo</c> schema is the DDL template
/// from which the provisioning service (<see cref="Karamchari.Core.Persistence.Provisioning.TenantProvisioningService"/>)
/// clones per-tenant schemas via <c>SELECT * INTO</c>.
/// </para>
/// <para>
/// <b>Commands (from <c>src/Backend/</c>)</b>
/// <code>
/// dotnet ef migrations add &lt;Name&gt; --project Karamchari.Payroll --startup-project Karamchari.Api
/// # Edit: replace [__tenant__] â†’ [dbo] in the generated migration file
/// dotnet ef database update          --project Karamchari.Payroll --startup-project Karamchari.Api
/// </code>
/// </para>
/// </remarks>
public sealed class PayrollDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PayrollDbContext>
{
    /// <inheritdoc/>
    public PayrollDbContext CreateDbContext(string[] args)
    {
        // Read the LocalDB connection string from appsettings.Development.json so the
        // factory stays in sync with the dev environment without hardcoding anything.
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Karamchari.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var connectionString = config.GetConnectionString("KaramchariDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:KaramchariDb not found in Karamchari.Api/appsettings.Development.json. " +
                "Ensure the API project is at the expected relative path.");

        var optionsBuilder = new DbContextOptionsBuilder<PayrollDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        // NOTE: Karamchari interceptors (TenantSchemaCommandInterceptor,
        // RlsSessionContextInterceptor, etc.) are intentionally NOT added here.
        // Migrations must emit plain SQL against [dbo]; the interceptors would
        // attempt to rewrite schema names and set SESSION_CONTEXT during migration
        // execution, which is both wrong and unsafe at design time.

        return new PayrollDbContext(optionsBuilder.Options, DesignTimeTenantProvider.Instance);
    }

    /// <summary>
    /// Stub <see cref="ITenantProvider"/> for EF design-time tooling.
    /// Returns a sentinel tenant that is never used in actual SQL execution
    /// because the interceptors are disabled in the design-time context.
    /// </summary>
    private sealed class DesignTimeTenantProvider : ITenantProvider
    {
        /// <summary>
        /// Provides required documentation for this member.
        /// </summary>
        public static readonly DesignTimeTenantProvider Instance = new();
        private static readonly TenantExecutionEnvelope DesignTimeTenant =
            new("design_time", "design_time_correlation", "design_time_request", ExecutionSource.Migration, TenantSource.Provisioning);

        /// <summary>
        /// Provides required documentation for this member.
        /// </summary>
        public string GetCurrentTenantId() => DesignTimeTenant.TenantId;

        public bool TryGetCurrentTenantId([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? tenantId)
        {
            tenantId = DesignTimeTenant.TenantId;
            return true;
        }

        public TenantExecutionEnvelope GetTenant() => DesignTimeTenant;

        /// <summary>
        /// Provides required documentation for this member.
        /// </summary>
        public bool TryGetTenant(out TenantExecutionEnvelope? tenant)
        {
            tenant = DesignTimeTenant;
            return true;
        }
    }
}
