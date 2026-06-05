namespace Karamchari.TimeAttendance.Migrations;

using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Design-time factory for <see cref="TimeAttendanceDbContext"/>.
/// Used exclusively by the EF Core CLI tools (<c>dotnet ef</c>) - never resolved
/// at runtime by the application's DI container.
/// </summary>
/// <remarks>
/// <para>
/// Migrations are generated with <c>dbo</c> schema (not <c>__tenant__</c>) because EF Core 10
/// does not support JSON columns on owned entities mapped to tables in a non-<c>dbo</c> schema
/// at design time. The <c>TenantSchemaCommandInterceptor</c> handles schema rewriting at runtime.
/// </para>
/// <para>
/// <b>Commands (from <c>src/Backend/</c>)</b>
/// <code>
/// dotnet ef migrations add &lt;Name&gt; --project Karamchari.TimeAttendance --startup-project Karamchari.Api
/// dotnet ef database update          --project Karamchari.TimeAttendance --startup-project Karamchari.Api
/// </code>
/// </para>
/// </remarks>
public sealed class TimeAttendanceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TimeAttendanceDbContext>
{
    private const string DesignTimeSchema = "dbo";

    /// <inheritdoc/>
    public TimeAttendanceDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Karamchari.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var connectionString = config.GetConnectionString("KaramchariDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:KaramchariDb not found in Karamchari.Api/appsettings.Development.json.");

        var optionsBuilder = new DbContextOptionsBuilder<TimeAttendanceDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        // Karamchari interceptors intentionally omitted - see PayrollDbContextDesignTimeFactory.

        // DesignTimeDbContext overrides ModelSchema so EF 10 can scaffold migrations even when
        // owned entities have JSON columns (unsupported in non-dbo schemas at design time).
        return new DesignTimeDbContext(optionsBuilder.Options, DesignTimeTenantProvider.Instance);
    }

    /// <summary>
    /// Thin subclass used only by dotnet ef tooling.
    /// Overrides ModelSchema to dbo so migrations are scaffolded against the physical schema.
    /// </summary>
    private sealed class DesignTimeDbContext(
        DbContextOptions<TimeAttendanceDbContext> options,
        ITenantProvider tenantProvider)
        : TimeAttendanceDbContext(options, tenantProvider)
    {
        protected override string ModelSchema => DesignTimeSchema;
    }

    private sealed class DesignTimeTenantProvider : ITenantProvider
    {
        public static readonly DesignTimeTenantProvider Instance = new();
        private static readonly TenantExecutionEnvelope DesignTimeTenant =
            new("design_time", "design-time-correlation", "design-time-request", ExecutionSource.Migration, TenantSource.Provisioning);

        public string GetCurrentTenantId() => DesignTimeTenant.TenantId;

        public bool TryGetCurrentTenantId([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? tenantId)
        {
            tenantId = DesignTimeTenant.TenantId;
            return true;
        }

        public TenantExecutionEnvelope GetTenant() => DesignTimeTenant;

        public bool TryGetTenant(out TenantExecutionEnvelope? tenant)
        {
            tenant = DesignTimeTenant;
            return true;
        }
    }
}
