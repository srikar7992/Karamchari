namespace Karamchari.TimeAttendance.Migrations;

using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Design-time factory for <see cref="TimeAttendanceDbContext"/>.
/// Used exclusively by the EF Core CLI tools (<c>dotnet ef</c>) — never resolved
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
/// dotnet ef migrations add &lt;Name&gt; --project Karamchari.TimeAttendance --startup-project Karamchari.Api
/// # Edit: replace [__tenant__] → [dbo] in the generated migration file
/// dotnet ef database update          --project Karamchari.TimeAttendance --startup-project Karamchari.Api
/// </code>
/// </para>
/// </remarks>
public sealed class TimeAttendanceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TimeAttendanceDbContext>
{
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
        // Karamchari interceptors intentionally omitted — see PayrollDbContextDesignTimeFactory.

        return new TimeAttendanceDbContext(optionsBuilder.Options, DesignTimeTenantProvider.Instance);
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
