namespace Karamchari.HR.Migrations;

using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.HR.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Design-time factory for <see cref="HRDbContext"/>.
/// Used exclusively by the EF Core CLI tools (<c>dotnet ef</c>) â€” never resolved
/// at runtime by the application's DI container.
/// </summary>
/// <remarks>
/// <para>
/// After running <c>dotnet ef migrations add &lt;Name&gt;</c>, open the generated
/// migration file and replace every <c>[__tenant__]</c> with <c>[dbo]</c>.
/// See <c>PayrollDbContextDesignTimeFactory</c> for the full workflow explanation.
/// </para>
/// <para>
/// <b>Commands (from <c>src/Backend/</c>)</b>
/// <code>
/// dotnet ef migrations add &lt;Name&gt; --project Karamchari.HR --startup-project Karamchari.Api
/// # Edit: replace [__tenant__] â†’ [dbo] in the generated migration file
/// dotnet ef database update          --project Karamchari.HR --startup-project Karamchari.Api
/// </code>
/// </para>
/// </remarks>
public sealed class HRDbContextDesignTimeFactory : IDesignTimeDbContextFactory<HRDbContext>
{
    /// <inheritdoc/>
    public HRDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Karamchari.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var connectionString = config.GetConnectionString("KaramchariDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:KaramchariDb not found in Karamchari.Api/appsettings.Development.json.");

        var optionsBuilder = new DbContextOptionsBuilder<HRDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        // Karamchari interceptors intentionally omitted â€” see PayrollDbContextDesignTimeFactory.

        return new HRDbContext(optionsBuilder.Options, DesignTimeTenantProvider.Instance);
    }

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
